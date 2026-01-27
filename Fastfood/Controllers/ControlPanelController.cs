using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using Fastfood.Data;
using Fastfood.Models;
using Fastfood.ViewModel;
using Fastfood.ViewModels;
using FastFood.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Fastfood.Controllers
{
	public class ControlPanelController : Controller
	{
        private readonly DataDbContext db;
        private IWebHostEnvironment env;
        public ControlPanelController(DataDbContext _db, IWebHostEnvironment _env)
        {
            db = _db;
            env = _env;
        }

        #region Dashboard
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            Console.WriteLine("[LowStockFilter] Executing filter...");

            // ✅ Direct query for low stock items (same as LowStock method but self-contained)
            var lowStockItems = await (
                from st in db.StockTracking
                where st.Source == "Purchase" || st.Source == "Sale"
                group st by new { st.ItemId, st.ItemName } into g
                let purchased = g.Where(x => x.Source == "Purchase").Sum(x => (int?)x.Qty) ?? 0
                let sold = g.Where(x => x.Source == "Sale").Sum(x => (int?)x.Qty) ?? 0
                let remaining = purchased - sold
                where remaining <= 10
                orderby remaining
                select new
                {
                    ItemId = g.Key.ItemId,
                    ItemName = g.Key.ItemName ?? "Unknown Item",
                    Remaining = remaining,
                    DetectedAt = DateTime.Now // ✅ stamp when this notification is created
                }
            ).ToListAsync();

            // Debug log
            Console.WriteLine($"[LowStockFilter] Found {lowStockItems.Count} low stock items.");
            foreach (var item in lowStockItems)
                Console.WriteLine($"   Item: {item.ItemName}, Remaining: {item.Remaining}, DetectedAt: {item.DetectedAt}");

            // ✅ Pass to _Layout
            if (context.Controller is Controller controller)
            {
                controller.ViewBag.LowStockNotifications = lowStockItems;
                controller.ViewBag.NotificationCount = lowStockItems.Count;
            }

            await next();
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // --- Session and access checks ---
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
            {
                return RedirectToAction("Login");
            }

            // --- Summary calculations ---
            var totalPurchases = await db.Inv_Purchases.CountAsync(); // total purchases from suppliers
            var totalSuppliers = await db.suppliers.CountAsync();
            //var totalItemsInStock = await db.Inv_PurchasedItems.SumAsync(pi => (decimal?)pi.Qty) ?? 0;
            var totalItemsInStock = await db.StockTracking
                .Where(st => st.Qty > 0)   // assuming you track current stock in this table
                .Select(st => st.ItemName)
                .Distinct()
                .CountAsync();


            // --- Best-selling items from SoldItems ---
            var bestSellingGroup = await db.soldItems
                .GroupBy(si => si.ItemName ?? "Unknown")
                .Select(g => new
                {
                    ItemName = g.Key,
                    TotalQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(g => g.TotalQty)
                .FirstOrDefaultAsync();

            var topSellingItems = await db.soldItems
                .GroupBy(si => si.ItemName ?? "Unknown")
                .Select(g => new
                {
                    ItemName = g.Key,
                    TotalQty = g.Sum(x => x.Qty)
                })
                .OrderByDescending(g => g.TotalQty)
                .Take(5)
                .ToListAsync();

            // --- Monthly sales trend from Sales table ---
            var monthlySalesData = await db.sales
                .Where(s => s.SaleDate.HasValue)
                .ToListAsync(); // fetch to memory

            var monthlyGroups = monthlySalesData
                .GroupBy(s => s.SaleDate.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(s => (decimal)(s.Payment ?? 0))
                })
                .OrderBy(g => g.Month)
                .ToList();

            // --- Prepare ViewModel ---
            var vm = new PurchaseStockDashboardVM
            {
                TotalPurchases = totalPurchases,
                TotalSuppliers = totalSuppliers,
                TotalItemsInStock = totalItemsInStock,
                BestSellingItem = bestSellingGroup?.ItemName ?? "N/A",
                BestSellingQty = (decimal)(bestSellingGroup?.TotalQty ?? 0),
                MonthlyLabels = monthlyGroups
                    .Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m.Month))
                    .ToList(),
                MonthlyPurchaseValues = monthlyGroups
                    .Select(m => m.TotalRevenue)
                    .ToList(),
                BestSellingLabels = topSellingItems
                    .Select(t => t.ItemName)
                    .ToList(),
                BestSellingValues = topSellingItems
                    .Select(t => (decimal)t.TotalQty)
                    .ToList()
            };

            return View(vm);
        }
        #endregion

        #region ErrorMessage
        public IActionResult ErrorMessage()
        {
            return View();
        }

        #endregion

        #region Category
        // GET: ControlPanelController
        [HttpGet]
        public async Task<IActionResult> CategoryDetails()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "CategoryDetails";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";
                    var categories = await db.categories.ToListAsync();
                    return View(categories);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        public IActionResult CreateCategory()
		{

			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "CreateCategory";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					return View();
				}
				else
				{
					TempData["Permission"] = "You do not have permission to access this page";
					return View();
				}
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}


		}

        // POST: ControlPanelController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(Category newcat)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        // Handle image upload
                        string uniqueFileName = null;
                        if (newcat.image != null)
                        {
                            string uploadsFolder = Path.Combine(env.WebRootPath, "Images");
                              
                            string fileExtension = Path.GetExtension(newcat.image.FileName);
                            uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                newcat.image.CopyTo(fileStream);
                            }
                        }

                        Category category = new()
                        {
                            CategoryName = newcat.CategoryName,
                            Root = 0,
                            Picture = uniqueFileName // Save file name in DB
                        };

                        db.categories.Add(category);
                        db.SaveChanges();

                        TempData["ToastType"] = "success";
                        TempData["ToastMessage"] = $"Category {category.CategoryName} added successfully";
                        return RedirectToAction(nameof(CategoryDetails));
                    }
                    catch
                    {
                        return View(newcat);
                    }
                }
                else
                {
                    return View(newcat); // Validation failed
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }


        // GET: ControlPanelController/Edit/5
        [HttpGet]
		public IActionResult UpdateCategory(int id)
		{
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "UpdateCategory";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					var category = db.categories.Find(id);
					return View(category);
				}
				else
				{
					TempData["Permission"] = "You do not have permission to access this page";
					return View();
				}
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}


		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCategory(Category updatedCat)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var category = db.categories.Find(updatedCat.CategoryId);
                        if (category != null)
                        {
                            category.CategoryName = updatedCat.CategoryName;
                            category.Root = updatedCat.Root;

                            if (updatedCat.image != null && updatedCat.image.Length > 0)
                            {
                                // Delete old image if exists and not shared with other records
                                if (!string.IsNullOrEmpty(category.Picture))
                                {
                                    var imageUsageCount = db.categories.Count(c => c.Picture == category.Picture);

                                    // If no other record uses this image, delete it
                                    if (imageUsageCount == 1)
                                    {
                                        var oldPath = Path.Combine(env.WebRootPath, "images", category.Picture);
                                        if (System.IO.File.Exists(oldPath))
                                        {
                                            System.IO.File.Delete(oldPath);
                                        }
                                    }
                                }

                                // Save new image
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(updatedCat.image.FileName);
                                var filePath = Path.Combine(env.WebRootPath, "images", fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    updatedCat.image.CopyTo(stream);
                                }

                                category.Picture = fileName;
                            }


                            db.categories.Update(category);
                            db.SaveChanges();

                            TempData["ToastType"] = "success";
                            TempData["ToastMessage"] = $"Category {category.CategoryName} updated successfully.";
                            return RedirectToAction(nameof(CategoryDetails));
                        }

                        return NotFound();
                    }
                    catch
                    {
                        return View(updatedCat);
                    }
                }
                else
                {
                    return View(updatedCat);
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

       

        public IActionResult DeleteCategory(int id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "DeleteCategory";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();

                if (permission)
                {
                    var RemoveCategory = db.categories.Find(id);

                    if (RemoveCategory != null)
                    {
                        // Delete the image file from wwwroot/images
                        if (!string.IsNullOrEmpty(RemoveCategory.Picture))
                        {
                            var imagePath = Path.Combine(env.WebRootPath, "images", RemoveCategory.Picture);
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }

                        db.categories.Remove(RemoveCategory);
                        db.SaveChanges();

                        TempData["ToastType"] = "error";
                        TempData["ToastMessage"] = $"Category {RemoveCategory.CategoryName} deleted successfully.";
                        return RedirectToAction(nameof(CategoryDetails));
                    }

                    return NotFound();
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        #endregion

        #region Items

        public async Task<IActionResult> ItemsDetail()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "ItemsDetail";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";
                    var items = await db.items.ToListAsync();
                    return View(items);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateItem()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "CreateItem";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";
                    ItemsVM items = new()
                    {
                        category = await db.categories.ToListAsync()
                    };
                    return View(items);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(ItemsVM item)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        string uniqueFileName = null;

                        if (item.ItemImage != null)
                        {
                            string uploadsFolder = Path.Combine(env.WebRootPath, "Images");
                            string fileExtension = Path.GetExtension(item.ItemImage.FileName);
                            uniqueFileName = Guid.NewGuid() + fileExtension;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await item.ItemImage.CopyToAsync(fileStream);
                            }
                        }

                        Item addItem = new()
                        {
                            ItemName = item.ItemName,
                            RecentUnitPrice = item.RecentUnitPrice,
                            Discount = item.Discount,
                            CategoryId = item.CategoryId,
                            Remarks = item.Remarks,
                            Picture = uniqueFileName,
                            ItemType = item.ItemType
                        };

                        await db.items.AddAsync(addItem);
                        await db.SaveChangesAsync();

                        TempData["ToastType"] = "success";
                        TempData["ToastMessage"] = $"Item {addItem.ItemName} added successfully";
                        return RedirectToAction(nameof(ItemsDetail));
                    }
                    catch
                    {
                        return View(item);
                    }
                }
                else
                {
                    return View(item);
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditItem(int id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "EditItem";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";
                    var item = await db.items.FindAsync(id);
                    if (item == null) return NotFound();

                    ItemsVM items = new()
                    {
                        ItemId = id,
                        ItemName = item.ItemName,
                        RecentUnitPrice = item.RecentUnitPrice,
                        Discount = item.Discount,
                        CategoryId = item.CategoryId,
                        Remarks = item.Remarks,
                        category = await db.categories.ToListAsync(),
                        Picture = item.Picture,
                        ItemType = item.ItemType
                    };

                    return View(items);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(ItemsVM item)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        var existingItem = await db.items.FindAsync(item.ItemId);
                        if (existingItem != null)
                        {
                            existingItem.ItemName = item.ItemName;
                            existingItem.RecentUnitPrice = item.RecentUnitPrice;
                            existingItem.Discount = item.Discount;
                            existingItem.CategoryId = item.CategoryId;
                            existingItem.Remarks = item.Remarks;
                            existingItem.ItemType = item.ItemType;

                            if (item.ItemImage != null && item.ItemImage.Length > 0)
                            {
                                if (!string.IsNullOrEmpty(existingItem.Picture))
                                {
                                    var usageCount = await db.items.CountAsync(i => i.Picture == existingItem.Picture);
                                    if (usageCount == 1)
                                    {
                                        var oldPath = Path.Combine(env.WebRootPath, "images", existingItem.Picture);
                                        if (System.IO.File.Exists(oldPath))
                                        {
                                            System.IO.File.Delete(oldPath);
                                        }
                                    }
                                }

                                var fileName = Guid.NewGuid() + Path.GetExtension(item.ItemImage.FileName);
                                var filePath = Path.Combine(env.WebRootPath, "images", fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await item.ItemImage.CopyToAsync(stream);
                                }

                                existingItem.Picture = fileName;
                            }

                            db.items.Update(existingItem);
                            await db.SaveChangesAsync();

                            TempData["ToastType"] = "success";
                            TempData["ToastMessage"] = $"Item {existingItem.ItemName} updated successfully.";
                            return RedirectToAction(nameof(ItemsDetail));
                        }

                        return NotFound();
                    }
                    catch
                    {
                        TempData["ToastType"] = "error";
                        TempData["ToastMessage"] = "An error occurred while updating the item.";
                        return View(item);
                    }
                }
                else
                {
                    TempData["ToastType"] = "warning";
                    TempData["ToastMessage"] = "Please correct the form errors.";
                    return View(item);
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        public async Task<IActionResult> DeleteItem(int id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "DeleteItem";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    var deleteItem = await db.items.FindAsync(id);

                    if (deleteItem != null)
                    {
                        if (!string.IsNullOrEmpty(deleteItem.Picture))
                        {
                            var imagePath = Path.Combine(env.WebRootPath, "images", deleteItem.Picture);
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }

                        db.items.Remove(deleteItem);
                        await db.SaveChangesAsync();

                        TempData["ToastType"] = "error";
                        TempData["ToastMessage"] = $"Item {deleteItem.ItemName} deleted successfully.";
                        return RedirectToAction(nameof(ItemsDetail));
                    }

                    return NotFound();
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        #endregion

        #region Consumebles
        // GET: List of Consumeables
        // ==================== LIST PAGE ====================
       public async Task<IActionResult> Consumeables()
            {
                TempData["UserName"] = HttpContext.Session.GetString("UserName");
                TempData["Access"] = HttpContext.Session.GetString("Access");

                if (HttpContext.Session.GetString("flag") != "true")
                    return RedirectToAction(nameof(Login));

                var methodName = "Consumeables";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (!permission)
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View(new List<Consumeable>());
                }

                TempData["Permission"] = "";

                // ✅ FIXED: include navigation property, not scalar FK
                var consumeables = await db.Consumeables
                    .Include(c => c.Unit)
                    .ToListAsync();

                return View(consumeables);
            }


        // ==================== CREATE PAGE (GET) ====================
        public IActionResult CreateConsumeable()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "CreateConsumeable";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefault();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return View();
            }

            TempData["Permission"] = "";

            // Fetch all Units for dropdown
            ViewBag.UnitList = db.UnitPrices
                .Select(u => new { u.UnitId, u.UnitCode })
                .ToList();

            return View();
        }

        // ==================== CREATE PAGE (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConsumeable(Consumeable model)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "CreateConsumeable";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return View(model);
            }

            TempData["Permission"] = "";

            if (ModelState.IsValid)
            {
                await db.Consumeables.AddAsync(model);
                await db.SaveChangesAsync();

                TempData["ToastMessage"] = "Consumeable created successfully.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Consumeables");
            }

            // Reload dropdown if form invalid
            ViewBag.UnitList = db.UnitPrices
                .Select(u => new { u.UnitId, u.UnitCode })
                .ToList();

            return View(model);
        }

        // ==================== EDIT PAGE (GET) ====================
        public async Task<IActionResult> UpdateConsumeable(int id)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "UpdateConsumeable";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return View();
            }

            TempData["Permission"] = "";

            var consumeable = await db.Consumeables.FirstOrDefaultAsync(c => c.CMID == id);
            if (consumeable == null) return NotFound();

            // Units dropdown
            ViewBag.UnitList = db.UnitPrices
                .Select(u => new { u.UnitId, u.UnitCode })
                .ToList();

            return View(consumeable);
        }

        // ==================== EDIT PAGE (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConsumeable(Consumeable model)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "UpdateConsumeable";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return View(model);
            }

            TempData["Permission"] = "";

            if (ModelState.IsValid)
            {
                db.Consumeables.Update(model);
                await db.SaveChangesAsync();

                TempData["ToastMessage"] = "Consumeable updated successfully.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Consumeables");
            }

            ViewBag.UnitList = db.UnitPrices
                .Select(u => new { u.UnitId, u.UnitCode })
                .ToList();

            return View(model);
        }

        // ==================== DELETE ====================
        public async Task<IActionResult> DeleteConsumeable(int id)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "DeleteConsumeable";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)    
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return RedirectToAction("Consumeables");
            }

            TempData["Permission"] = "";

            var consumeable = await db.Consumeables.FirstOrDefaultAsync(c => c.CMID == id);
            if (consumeable == null) return NotFound();

            db.Consumeables.Remove(consumeable);
            await db.SaveChangesAsync();

            TempData["ToastMessage"] = "Consumeable deleted successfully.";
            TempData["ToastType"] = "error";
            return RedirectToAction("Consumeables");
        }
        #endregion

        #region StockTracking

        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "LowStock";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return View();
            }

            TempData["Permission"] = "";

            // 🧮 Include Consumption in calculation
            var lowStockReport = await db.StockTracking
                .GroupBy(st => new { st.ItemId, st.ItemName })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.ItemName,
                    Purchased = g.Where(x => x.Source != null && x.Source.StartsWith("Purchase"))
                                 .Sum(x => (decimal?)x.Qty) ?? 0m,

                    SoldSum = g.Where(x => x.Source == "Sale")
                               .Sum(x => (decimal?)x.Qty) ?? 0m,

                    ConsumedSum = g.Where(x => x.Source == "Consumption" || x.Source == "Sale-Consumption")
                                   .Sum(x => (decimal?)x.Qty) ?? 0m
                })
                .Select(x => new StockTrackingVM
                {
                    ItemId = x.ItemId,
                    ItemName = x.ItemName ?? "Unknown Item",
                    Purchased = x.Purchased,
                    Sold = x.SoldSum,
                    Consumed = x.ConsumedSum,
                    Remaining = x.Purchased - (x.SoldSum + x.ConsumedSum)
                })
                .Where(x => x.Remaining <= 10)
                .OrderBy(x => x.Remaining)
                .ToListAsync();

            return View(lowStockReport);
        }


        [HttpGet]
        public async Task<IActionResult> StockTracking()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction(nameof(Login));

            var methodName = "StockTracking";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to access this page";
                return View();
            }

            TempData["Permission"] = "";

            // 🧮 Include Consumption in stock calculations
            var stockReport = await db.StockTracking
                .GroupBy(st => new { st.ItemId, st.ItemName })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.ItemName,
                    Purchased = g.Where(x => x.Source != null && x.Source.StartsWith("Purchase"))
                                 .Sum(x => (decimal?)x.Qty) ?? 0m,

                    SoldSum = g.Where(x => x.Source == "Sale")
                               .Sum(x => (decimal?)x.Qty) ?? 0m,

                    ConsumedSum = g.Where(x => x.Source == "Consumption" || x.Source == "Sale-Consumption")
                                   .Sum(x => (decimal?)x.Qty) ?? 0m
                })
                .Select(x => new StockTrackingVM
                {
                    ItemId = x.ItemId,
                    ItemName = x.ItemName ?? "Unknown Item",
                    Purchased = x.Purchased,
                    Sold = x.SoldSum,
                    Consumed = x.ConsumedSum,
                    Remaining = x.Purchased - (x.SoldSum + x.ConsumedSum)
                })
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            // ✅ Count low stock items (<=10)
            ViewBag.LowStockCount = stockReport.Count(x => x.Remaining <= 10);

            return View(stockReport);
        }

        #endregion
         

        #region Purchase-Products
        [HttpGet]
     
        public async Task<IActionResult> AllPurchases()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
            {
                return RedirectToAction("Login", "ControlPanel");
            }

            var purchases = await db.Inv_Purchases
                .Include(p => p.Supplier)                     // ✅ Load Supplier
                .Include(p => p.PurchasedItems)           // ✅ Load purchased items list
                .OrderByDescending(x => x.PurchaseId)
                .ToListAsync();

            return View(purchases);
        }


        [HttpGet]
        public async Task<JsonResult> GetItemPrices(int itemId)
        {
            var item = await db.items.FirstOrDefaultAsync(i => i.ItemId == itemId);
            if (item != null)
            {
                return Json(new
                {
                    unitPrice = item.RecentUnitPrice ?? 0,
                    sellingPrice = item.RetailPrice ?? 0
                });
            }

            return Json(new { unitPrice = 0, sellingPrice = 0 });
        }

        [HttpGet]
        public async Task<IActionResult> CreatePurchase()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "CreatePurchase";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";

                    var suppliers = await db.suppliers
                        .Select(s => new SelectListItem
                        {
                            Value = s.SupplierId.ToString(),
                            Text = s.Name
                        }).ToListAsync();

                    var items = await db.items
                        .Where(i => i.ItemType == "Beverages" || i.ItemType == "Consumable")
                        .Select(i => new SelectListItem
                        {
                            Value = i.ItemId.ToString(),
                            Text = i.ItemName
                        }).ToListAsync();
                    var consumeables = await db.Consumeables
                   .Select(c => new SelectListItem
                   {
                       Value = c.CMID.ToString(),
                       Text = c.CMName,
                       // Use Text property only for display, we’ll add UnitPrice separately in a custom object
                   }).ToListAsync();

                    // Pass a dictionary for UnitPrice as ViewBag
                    ViewBag.ConsumeablePrices = db.Consumeables
                        .Select(c => new { c.CMID, UnitPrice = c.UnitPrice ?? 0 })
                        .ToDictionary(c => c.CMID.ToString(), c => c.UnitPrice);



                    var model = new PurchaseVM
                    {
                        PurchaseDate = DateTime.Now,
                        PurchasedItems = new List<PurchasedItemVM> { new PurchasedItemVM() },
                        Suppliers = suppliers,
                        Items = items,
                        Consumeables = consumeables 
                    };


                    return View(model);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchase(PurchaseVM model)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") != "true")
            {
                return RedirectToAction(nameof(Login));
            }

            var methodName = "CreatePurchase";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = await db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefaultAsync();

            if (!permission)
            {
                TempData["Permission"] = "You do not have permission to perform this action";
                return View(model);
            }

            model.DealingPerson = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(model.DealingPerson))
            {
                TempData["Error"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Account");
            }

            await PopulateDropdownsAsync(model);

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }
             
            string purchaseType = model.OrderOrpurchase;
            var random = new Random();
            var invoiceNo = random.Next(100000, 999999);

            // --- Save main purchase record ---
            var purchase = new Inv_Purchase
            {
                SupplierId = model.SupplierId,
                PurchaseDate = model.PurchaseDate,
                InvoiceNo = invoiceNo,
                Payment = model.Payment,
                FlatDisc = model.FlatDisc,
                Misc = model.Misc,
                Commesion = model.Commesion,
                DealingPerson = model.DealingPerson,
                OrderOrpurchase = purchaseType
            };

            await db.Inv_Purchases.AddAsync(purchase);
            await db.SaveChangesAsync();

            // --- Save purchased items ---
            foreach (var item in model.PurchasedItems.Where(x => x != null))
            {
                if (item.IsConsumeable)
                {
                    // update consumeables stock
                    var consumeable = await db.Consumeables.FirstOrDefaultAsync(c => c.CMID == item.ItemId);
                    if (consumeable != null)
                    {
                        // Update stock and price
                        consumeable.UnitPrice = item.UnitPrice;
                        consumeable.StockAletQty = (consumeable.StockAletQty ?? 0) + item.Qty;

                        db.Consumeables.Update(consumeable);

                        // Log stock movement
                        var stock = new StockTracking
                        {
                            TrsID = purchase.PurchaseId,
                            TrsDate = purchase.PurchaseDate,
                            ItemId = consumeable.CMID,
                            Qty = item.Qty,
                            UnitId = consumeable.UnitId ?? default, // ensure Consumeables has UnitId
                            Source = "Purchase",
                            Price = item.UnitPrice,
                            ItemName = consumeable.CMName
                        };
                       

                        await db.StockTracking.AddAsync(stock);
                    }
                }
                else
                {
                    // Save normal Inv_Item purchase
                    var purchasedItem = new Inv_PurchasedItems
                    {
                        PurchaseId = purchase.PurchaseId,
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        PurchaseType = purchaseType,
                        Qty = item.Qty,
                        UnitPrice = item.UnitPrice
                    };
                    await db.Inv_PurchasedItems.AddAsync(purchasedItem);

                    var itemName = (await db.items.FirstOrDefaultAsync(i => i.ItemId == item.ItemId))?.ItemName ?? "";

                    var stock = new StockTracking
                    {
                        TrsID = purchase.PurchaseId,
                        TrsDate = purchase.PurchaseDate,
                        ItemId = item.ItemId,
                        Qty = item.Qty,
                        Source = "Purchase",
                        Price = item.UnitPrice,
                        ItemName = itemName
                    };
                    await db.StockTracking.AddAsync(stock);
                }
            }

            await db.SaveChangesAsync();

            TempData["ToastMessage"] = "Purchase created successfully!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(StockTracking));
        }


       

        private async Task PopulateDropdownsAsync(PurchaseVM model)
        {
            model.Suppliers = await db.suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.Name
                }).ToListAsync();

            model.Items = await db.items
                .Select(i => new SelectListItem
                {
                    Value = i.ItemId.ToString(),
                    Text = i.ItemName
                }).ToListAsync();

            var consumeables = await db.Consumeables.ToListAsync();

            model.Consumeables = await db.Consumeables
          .Select(c => new SelectListItem
          {
              Value = c.CMID.ToString(),
              Text = c.CMName
          }).ToListAsync();

            ViewBag.ConsumeablePrices = await db.Consumeables
                .ToDictionaryAsync(c => c.CMID.ToString(), c => c.UnitPrice ?? 0);
        }

        #region UnitPrice_Table
        // GET: Index / List all units
        public IActionResult UnitPrices()
        {
            var units = db.UnitPrices.ToList();
            return View(units);
        }

        // GET: Create
        public IActionResult CreateUnitPrice()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUnitPrice(UnitPrice unit)
        {
            if (ModelState.IsValid)
            {
                db.UnitPrices.Add(unit);
                db.SaveChanges();
                TempData["ToastMessage"] = "Unit created successfully!";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            return View(unit);
        }

        // GET: Edit
        public IActionResult EditUnitPrice(int id)
        {
            var unit = db.UnitPrices.Find(id);
            if (unit == null) return NotFound();
            return View(unit);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUnitPrice(UnitPrice unit)
        {
            if (ModelState.IsValid)
            {
                db.UnitPrices.Update(unit);
                db.SaveChanges();
                TempData["ToastMessage"] = "Unit updated successfully!";
                TempData["ToastType"] = "success";
                return RedirectToAction(nameof(Index));
            }
            return View(unit);
        }

        // GET: Delete
        public IActionResult DeleteUnitPrice(int id)
        {
            var unit = db.UnitPrices.Find(id);
            if (unit == null) return NotFound();

            db.UnitPrices.Remove(unit);
            db.SaveChanges();
            TempData["ToastMessage"] = "Unit deleted successfully!";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }
        #endregion


        #endregion

        #region RawMaterialConsumption
        public IActionResult RawMaterialConsumption()
        {
            var data = db.RawMaterial_Items_Consumption
                         .Include(r => r.Unit)  // ✅ OK
                         .Include(r => r.Item)  // ✅ Correct - Don't do r.Item.ItemId
                         .ToList();

            return View(data);
        }



        // Create GET
        public IActionResult CreateRawMaterialConsumption()
        {
            var vm = new RawMaterialConsumptionVM
            {
                Consumptions = new List<RawMaterial_Items_Consumption>
        {
            new RawMaterial_Items_Consumption() // one empty row for the form
        },
                Consumeables = db.Consumeables.ToList(),
                BaseItems = db.items.ToList()
            };

            return View(vm);
        }


        // Create POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRawMaterialConsumption(RawMaterialConsumptionVM vm, int BaseItemId)
        {
            if (!vm.Consumptions.Any())
            {
                ModelState.AddModelError("", "At least one raw material is required.");
            }

            if (ModelState.IsValid)
            {
                foreach (var item in vm.Consumptions)
                {
                    var raw = db.Consumeables.FirstOrDefault(c => c.CMID == item.RMInv_ItemId);
                    if (raw == null) continue;

                    item.RMItemName = raw.CMName;
                    item.UnitId = raw.UnitId;

                    // Assign the single base item to all entries
                    item.BInv_ItemId = BaseItemId;

                    db.RawMaterial_Items_Consumption.Add(item);
                }

                db.SaveChanges();
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = $"Record(s) have been added successfully.";
                return RedirectToAction(nameof(RawMaterialConsumption));
            }

            vm.Consumeables = db.Consumeables.ToList();
            vm.BaseItems = db.items.ToList();
            return View(vm);
        }


        [HttpGet]
        public IActionResult EditRawMaterialConsumption(int Id)
        {
            if (Id == 0)
                return BadRequest("Base Item ID not provided.");

            //Fetch Base Item Name
            var name = db.items
                .Where(i => i.ItemId == Id)
                .Select(i => i.ItemName)
                .FirstOrDefault() ?? "Unknown Item";
            ViewBag.BaseItemName = name;
            // Fetch all raw material consumptions linked to this base item
            var consumptions = db.RawMaterial_Items_Consumption
                                 .Where(x => x.BInv_ItemId == Id)
                                 .ToList();

            // If no consumption exists, initialize one empty record
            if (!consumptions.Any())
            {
                consumptions.Add(new RawMaterial_Items_Consumption
                {
                    BInv_ItemId = Id
                });
            }
            else
            {
                // Populate RMItemName and Unit info from Consumeables
                foreach (var c in consumptions)
                {
                    if (c.RMInv_ItemId.HasValue)
                    {
                        var raw = db.Consumeables.FirstOrDefault(x => x.CMID == c.RMInv_ItemId.Value);
                        if (raw != null)
                        {
                            c.RMItemName = raw.CMName;
                            c.UnitId = raw.UnitId;
                            c.Unit = db.UnitPrices.FirstOrDefault(u => u.UnitId == raw.UnitId);
                        }
                    }
                }
            }

            // Prepare ViewModel
            var vm = new RawMaterialConsumptionVM
            {
                Consumptions = consumptions,
                Consumeables = db.Consumeables.ToList(),
                BaseItems = db.items.ToList()
            };

            return View("EditRawMaterialConsumption", vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRawMaterialConsumption(RawMaterialConsumptionVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Consumeables = db.Consumeables.ToList();
                vm.BaseItems = db.items.ToList();
                return View(vm);
            }

            foreach (var item in vm.Consumptions)
            {
                if (item.S_No > 0) // Existing record
                {
                    var existing = db.RawMaterial_Items_Consumption
                                     .FirstOrDefault(x => x.S_No == item.S_No);

                    if (existing == null)
                        continue;

                    if (item.IsDeleted)
                    {
                        db.RawMaterial_Items_Consumption.Remove(existing);
                        continue;
                    }

                    // Update fields
                    existing.RMInv_ItemId = item.RMInv_ItemId;
                    existing.RMQTY = item.RMQTY;
                    existing.Remarks = item.Remarks;

                    var consumeable = db.Consumeables
                                        .FirstOrDefault(c => c.CMID == item.RMInv_ItemId);

                    if (consumeable != null)
                    {
                        existing.RMItemName = consumeable.CMName;
                        existing.UnitId = consumeable.UnitId;
                    }
                }
                else // New record
                {
                    if (item.RMInv_ItemId == null || item.RMQTY <= 0)
                        continue; // Skip empty new rows

                    var consumeable = db.Consumeables
                                        .FirstOrDefault(c => c.CMID == item.RMInv_ItemId);

                    if (consumeable != null)
                    {
                        item.RMItemName = consumeable.CMName;
                        item.UnitId = consumeable.UnitId;
                    }

                    // Set the BaseItemId if not already set
                    if (!item.BInv_ItemId.HasValue && vm.Consumptions.Any(c => c.BInv_ItemId.HasValue))
                    {
                        item.BInv_ItemId = vm.Consumptions.First(c => c.BInv_ItemId.HasValue).BInv_ItemId;
                    }
                 
                    db.RawMaterial_Items_Consumption.Add(item);
                }
            }
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Record Has Been updated successfully";
            db.SaveChanges(); // Commit all changes
            return RedirectToAction(nameof(RawMaterialConsumption));
        }





        public IActionResult DeleteRawMaterialConsumption(int id)
        {
            var records = db.RawMaterial_Items_Consumption.Where(x=>x.BInv_ItemId==id).ToList();
            if (records == null) return NotFound();

            db.RawMaterial_Items_Consumption.RemoveRange(records);
            db.SaveChanges();
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"Record Has Been Deleted successfully";
            return RedirectToAction(nameof(RawMaterialConsumption));
        }

        #endregion

        #region Method
        [HttpGet]
        public async Task<IActionResult> MethodDetail()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "MethodDetail";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";
                    var methods = await db.methods.ToListAsync();
                    return View(methods);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }
        public async Task<IActionResult> CreateMethod()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var userCode = HttpContext.Session.GetString("UserCode");
                string methodname = "CreateMethod";

                var permission = await db.userPermissions
                    .FirstOrDefaultAsync(u => u.UserCode.ToString() == userCode && u.MethodName == methodname);

                if (permission?.View == true)
                {
                    TempData["DeniedMessage"] = "";
                    return View();
                }
                else
                {
                    TempData["DeniedMessage"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMethod(Method method)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                // Step 1: Add the new method
                Method _method = new()
                {
                    MethodName = method.MethodName
                };
                await db.methods.AddAsync(_method);
                await db.SaveChangesAsync();

                // Step 2: Get all user codes
                var allUserCodes = await db.logins
                    .Select(u => u.UserCode)
                    .ToListAsync();

                // Step 3: Add a permission entry for each user
                foreach (var userCode in allUserCodes)
                {
                    UserPermissions newPermission = new()
                    {
                        UserCode = userCode,
                        MethodId = _method.MethodId,
                        MethodName = _method.MethodName,
                        View = false
                    };
                    await db.userPermissions.AddAsync(newPermission);
                }

                // Step 4: Save permissions
                await db.SaveChangesAsync();

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = $"Method {method.MethodName} added successfully";
                return RedirectToAction(nameof(MethodDetail));
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateMethod(int Id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var userCode = HttpContext.Session.GetString("UserCode");
                string methodname = "UpdateMethod";

                var permission = await db.userPermissions
                    .FirstOrDefaultAsync(u => u.UserCode.ToString() == userCode && u.MethodName == methodname);

                if (permission?.View == true)
                {
                    var method = await db.methods.FindAsync(Id);
                    return View(method);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMethod(Method method)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                Method _method = new()
                {
                    MethodId = method.MethodId,
                    MethodName = method.MethodName
                };
                db.methods.Update(_method);

                var matchingPermissions = await db.userPermissions
                    .Where(up => up.MethodId == method.MethodId)
                    .ToListAsync();

                foreach (var permission in matchingPermissions)
                {
                    permission.MethodName = method.MethodName;
                }

                await db.SaveChangesAsync();

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = $"Method {method.MethodName} and related permissions updated successfully";

                return RedirectToAction(nameof(MethodDetail));
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        public async Task<IActionResult> DeleteMethod(int Id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "DeleteMethod";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = await db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefaultAsync();

                if (permission)
                {
                    TempData["Permission"] = "";

                    var method = await db.methods.FindAsync(Id);
                    if (method != null)
                    {
                        var permissionsToDelete = await db.userPermissions
                            .Where(p => p.MethodId == Id)
                            .ToListAsync();

                        db.userPermissions.RemoveRange(permissionsToDelete);
                        db.methods.Remove(method);

                        await db.SaveChangesAsync();

                        TempData["ToastType"] = "success";
                        TempData["ToastMessage"] = $"Method {method.MethodName} and related permissions deleted successfully";
                    }

                    return RedirectToAction(nameof(MethodDetail));
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }



        #endregion

        #region Register
        public IActionResult Register()
		{
			if (HttpContext.Session.GetString("flag") == "true")
			{
				var access = HttpContext.Session.GetString("Access");
				var userID = HttpContext.Session.GetString("UserID");
				var Password = HttpContext.Session.GetString("Password");

				var loggers = db.logins.Where(u => u.ID == userID && u.Password == Password && u.Access == access).FirstOrDefault();

				string Access = "Admin";

				if (loggers.Access.TrimEnd() == Access)
				{
					TempData["RegisterError"] = "";
					return View();
				}
				else
				{
					TempData["RegisterError"] = "You have no permission to Register any user";
					return View();
				}
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}

		}
        [HttpPost]
        public IActionResult Register(Register register)
        {
            // Step 1: Generate new user with a unique UserCode
            Guid generatedUserCode = Guid.NewGuid();

            Register newuser = new()
            {
                UserCode = generatedUserCode,
                ID = register.ID,
                Password = register.Password,
                Name = register.Name,
                Access = register.Access
            };

            // Step 2: Add the user to the database first
            db.logins.Add(newuser);
            db.SaveChanges(); // Save to ensure user exists in DB

            // Step 3: Fetch all methods from Methods table
            var methods = db.methods.ToList();

            // Step 4: Create permissions for all methods
            List<UserPermissions> permissions = new List<UserPermissions>();
            foreach (var item in methods)
            {
                UserPermissions permit = new()
                {
                    UserCode = generatedUserCode,
                    MethodId = item.MethodId,
                    MethodName = item.MethodName,
                    View = false // or true if you want to give access by default
                };
                permissions.Add(permit);
            }

            // Step 5: Add all permissions to the DB
            db.userPermissions.AddRange(permissions);
            db.SaveChanges();

            // Step 6: Done!
            TempData["alertmessage"] = "User Created Successfully";
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Login

        public IActionResult Login()
		{
			return View();
		}
		[HttpPost]
		public IActionResult Login(LoginViewModel logeduser)
		{
			if (ModelState.IsValid)
			{
				var user = db.logins.Where(x => x.ID == logeduser.ID && x.Password == logeduser.Password).FirstOrDefault();
				if (user != null)
				{
					HttpContext.Session.SetString("UserID", user.ID);
					HttpContext.Session.SetString("Password", user.Password);
					HttpContext.Session.SetString("Access", user.Access);
					HttpContext.Session.SetString("UserCode", user.UserCode.ToString());
					HttpContext.Session.SetString("UserName", user.Name);
					HttpContext.Session.SetString("flag", "true");
					TempData["UserName"] = user.Name;
					TempData["Access"] = user.Access;
					TempData["loginmessage"] = "Welcome To the System";
					return RedirectToAction("Dashboard");

				}
				else
				{
					TempData["InvalidCredentials"] = "Invalid UserID or Password";
					return View();
				}
			}
			else
			{
				TempData["EmptyCredentials"] = "Please Enter UserId and Password to Login";
				return View();
			}


		}
		#endregion

		#region Logout

		public IActionResult Logout()
		{
			HttpContext.Session.Clear();
			return RedirectToAction(nameof(Login));
		}
		#endregion

		#region User Permissions
		[HttpGet]
        public async Task<IActionResult> UsersDetail()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var permission = HttpContext.Session.GetString("Access");

                if (permission?.TrimEnd() == "Admin")
                {
                    var users = await db.logins.ToListAsync();
                    TempData["Permission"] = "";
                    return View(users);
                }
                else
                {
                    TempData["Permission"] = "Access Denied\nOnly Admin can Access it";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignPermissions(Guid id)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var permission = HttpContext.Session.GetString("Access");

                if (permission?.TrimEnd() == "Admin")
                {
                    var user = await db.logins.FindAsync(id);

                    if (user == null)
                    {
                        TempData["Permission"] = "User not found.";
                        return RedirectToAction("UsersDetail");
                    }
 
                    var permissions = await db.userPermissions
                                              .Where(u => u.UserCode == id)
                                              .ToListAsync();

                    AssignPermissionVM permissionVM = new AssignPermissionVM
                    {
                        user = user,
                        permissions = permissions  
                    };

                    TempData["Permission"] = "";
                    return View(permissionVM);
                }
                else
                {
                    TempData["Permission"] = "Access Denied. Only Admin can Access it.";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignPermissions(AssignPermissionVM permission)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                List<UserPermissions> permits = new();

                foreach (var item in permission.permissions)
                {
                    permits.Add(new UserPermissions
                    {
                        PermissionId = item.PermissionId,
                        UserCode = item.UserCode,
                        MethodId = item.MethodId,
                        MethodName = item.MethodName,
                        View = item.View
                    });
                }

                db.userPermissions.UpdateRange(permits);
                await db.SaveChangesAsync();

                var firstMethod = permission.permissions.FirstOrDefault()?.MethodName;
                var userCode = permission.permissions.FirstOrDefault()?.UserCode;

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = $"Permissions  updated successfully!";

                return RedirectToAction(nameof(AssignPermissions), new { id = userCode });
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }


        #endregion

        #region Users

        public IActionResult UserList()
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
			{
				var permission = HttpContext.Session.GetString("Access");
				if (permission.TrimEnd() == "Admin")
				{
					var users = db.logins.ToList();
					TempData["Permission"] = "";
					return View(users);
				}
				else
				{
					TempData["Permission"] = "Access Denied\nOnly Admin can Access it";
					return View();
				}
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}

		}

		public IActionResult EditUser(Guid id)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
			{
				var permission = HttpContext.Session.GetString("Access");
				if (permission.TrimEnd() == "Admin")
				{
					var user = db.logins.Find(id);
					TempData["Permission"] = "";
					return View(user);
				}
				else
				{
					TempData["Permission"] = "Access Denied\nOnly Admin can Access it";
					return View();
				}
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}

		}
		[HttpPost]
		public IActionResult EditUser(Register user)
		{
			Register Uuser = new();
			Uuser.UserCode = user.UserCode;
			Uuser.ID = user.ID;
			Uuser.Password = user.Password;
			Uuser.Name = user.Name;
			Uuser.Access = user.Access;
			db.logins.Update(Uuser);
			db.SaveChanges();
			return RedirectToAction(nameof(UserList));
		}

        #endregion

        #region Suppliers

        public IActionResult Suppliers()
        {

            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "Suppliers";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
                if (permission)
                {
                    TempData["Permission"] = "";
                    var suppliers = db.suppliers.ToList();
                    return View(suppliers);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        // GET: Create Supplier
        public IActionResult CreateSuppliers()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            // Check if user is logged in
            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "CreateSuppliers";
                var usercode = HttpContext.Session.GetString("UserCode");

                var permission = db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefault();

                if (permission)
                {
                    TempData["Permission"] = "";
                    return View();
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page.";
                    return RedirectToAction("AccessDenied"); 
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }


        // POST: Create Supplier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSuppliers(Suppliers newcat)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
            {
                if (!ModelState.IsValid)
                {
                    return View(newcat);
                }

                Suppliers suppliers = new Suppliers
                {
                    Name = newcat.Name,
                    Address = newcat.Address,
                    MobileNo = newcat.MobileNo,
                    SupplierCreationDate = DateTime.Now,
                    NIC = newcat.NIC,
                    Email = newcat.Email,
                    Citycode = newcat.Citycode,
                    Countrycode = newcat.Countrycode,
                    PhoneNo = newcat.PhoneNo,
                    Accountid = new Random().Next(100000, 999999), // Random 6-digit Account ID
                    DbStatus = true,
                    Operation_Type = newcat.Operation_Type
                };

                db.suppliers.Add(suppliers);
                db.SaveChanges();
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Supplier added successfully!";
                return RedirectToAction("Suppliers");
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        public IActionResult UpdateSupplier(int Id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var userCode = HttpContext.Session.GetString("UserCode");
                string methodname = "UpdateSupplier";

                var permission = db.userPermissions
                    .FirstOrDefault(u => u.UserCode.ToString() == userCode && u.MethodName == methodname);

                if (permission != null && permission.View)
                {
                    var suppliers = db.suppliers.Find(Id);
                    return View(suppliers);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }



        [HttpPost]
        public IActionResult UpdateSupplier(Suppliers supplier)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                if (ModelState.IsValid)
                {
                    var existingSupplier = db.suppliers.Find(supplier.SupplierId);
                    if (existingSupplier != null)
                    {
                        // Update only the allowed/modifiable fields
                        existingSupplier.Name = supplier.Name;
                        existingSupplier.Address = supplier.Address;
                        existingSupplier.PhoneNo = supplier.PhoneNo;
                        existingSupplier.MobileNo = supplier.MobileNo;
                        existingSupplier.SupplierCreationDate = supplier.SupplierCreationDate;
                        existingSupplier.NIC = supplier.NIC;
                        existingSupplier.Email = supplier.Email;
                        existingSupplier.Citycode = supplier.Citycode;
                        existingSupplier.Countrycode = supplier.Countrycode;
                        existingSupplier.Accountid = supplier.Accountid;
                        existingSupplier.DbStatus = supplier.DbStatus;
                        existingSupplier.ByDefault = supplier.ByDefault;
                        existingSupplier.Modifier = HttpContext.Session.GetString("UserName");

                        existingSupplier.Operation_Type = supplier.Operation_Type;

                        db.suppliers.Update(existingSupplier);
                        db.SaveChanges();
                        TempData["ToastType"] = "success";
                        TempData["ToastMessage"] = $"Update complete: {existingSupplier.Name} is now up to date.";
                        return RedirectToAction("Suppliers");

                    }
                    else
                    {
                        TempData["Error"] = "Supplier not found.";
                        return RedirectToAction(nameof(Suppliers));
                    }
                }
                else
                {
                    TempData["Error"] = "Validation failed.";
                    return View(supplier);
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        public IActionResult DeleteSupplier(int Id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "DeleteSupplier";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
                if (permission)
                {
                    TempData["Permission"] = "";
                    var supplier = db.suppliers.Find(Id);
                    db.suppliers.Remove(supplier);
                    db.SaveChanges();
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = $"Deleted successfully: {supplier.Name} has been removed.";
                    return RedirectToAction("Suppliers");

                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }

        #endregion


        #region Profile
        public async Task<IActionResult> Profile()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            var userId = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            Guid userGuid = Guid.Parse(userId);
            var user = await db.logins.FirstOrDefaultAsync(u => u.UserCode == userGuid);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // GET: Show the change password form
        public async Task<IActionResult> UpdateProfile()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");

            string userCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(userCode))
                return RedirectToAction("Login");

            Guid guid = Guid.Parse(userCode);
            var user = await db.logins.FirstOrDefaultAsync(u => u.UserCode == guid);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(Register register, string currentPassword, string newPassword, string confirmPassword)
        {
            string userCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(userCode))
                return RedirectToAction("Login");

            Guid guid = Guid.Parse(userCode);
            var user = await db.logins.FirstOrDefaultAsync(u => u.UserCode == guid);
            if (user == null)
                return NotFound();

            if (user.Password != currentPassword)
            {
                TempData["Error"] = "Current password is incorrect.";
                TempData["ToastType"] = "error";
                return View(user);
            }

            if (newPassword == currentPassword)
            {
                TempData["Error"] = "New password cannot be the same as the current password.";
                TempData["ToastType"] = "error";
                return View(user);
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                TempData["ToastType"] = "error";
                return View(user);
            }

            user.Password = newPassword;
            await db.SaveChangesAsync();

            TempData["PasswordUpdated"] = "Password updated successfully";
            TempData["ToastType"] = "success";
            return RedirectToAction("Profile");
        }



        #endregion


        #region Contact

        public IActionResult Contact()
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            return View();
		}
        #endregion


        #region Sales Reports Page

        public IActionResult ReportsDashboard()
        {
            return View();
        }
        #endregion

        #region Customer History  Page

        public async Task<IActionResult> CustomerHistory()
        {
            // Load all customers
            var customers = await db.clients
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(customers); // pass to Razor view
        }
        #endregion
    }
}
