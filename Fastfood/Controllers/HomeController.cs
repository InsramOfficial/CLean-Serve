using Fastfood.Data;
using Fastfood.Models;
using Fastfood.Services;
using Fastfood.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Xml;

namespace Fastfood.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataDbContext db;
        public HomeController(DataDbContext _db)
        {
            db = _db;

        }
        #region HomeIndex 
        [HttpGet]
        public async Task<IActionResult> HomeIndex()
        {
            List<Category> categories = new();
            List<Item> randomItems = new();
            List<Item> latestItems = new();

            try
            {
                categories = db.categories.ToList();

                randomItems = await db.items
                                      .AsNoTracking()
                                      .OrderBy(i => Guid.NewGuid())
                                      .Take(10)
                                      .ToListAsync();

                latestItems = await db.items
                                      .AsNoTracking()
                                      .OrderByDescending(i => i.ItemId)
                                      .Take(10)
                                      .ToListAsync();
            }
            catch (Exception ex)
            {
                // Optional: log the error
                // You can also set fallback/defaults here if needed
            }

            var viewModel = new HomeIndexVM
            {
                Categories = categories,
                RandomItems = randomItems,
                LatestItems = latestItems
            };

            return View(viewModel);
        }
        #endregion

        #region Login
        [HttpPost]
        public IActionResult Register(Client client)
        {
            if (ModelState.IsValid)
            {
                // You may want to check if email already exists
                var existing = db.clients.FirstOrDefault(c => c.Email == client.Email);
                if (existing != null)
                {
                    TempData["RegisterError"] = "Email already registered.";
                    return Redirect(Request.Headers["Referer"].ToString());
                }

                db.clients.Add(client);
                db.SaveChanges();

                TempData["ToastMessage"] = "Registration successful. Please login.";
                TempData["ToastType"] = "success";
                TempData.Keep();
                return Redirect(Request.Headers["Referer"].ToString());
            }
             
            TempData["ToastMessage"] = "Invalid data.";
            TempData["ToastType"] = "error";
            TempData.Keep();
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public IActionResult Login(Client model)
        {
            var user = db.clients
                         .FirstOrDefault(c => c.Email.Trim().ToLower() == model.Email.Trim().ToLower()
                                           && c.Password == model.Password);

            if (user != null)
            {
                HttpContext.Session.SetString("ClientName", user.Name ?? "Guest");
                HttpContext.Session.SetString("ClientId", user.Clientid.ToString());

                TempData["ToastMessage"] = $"Welcome {user.Name}";
                TempData["ToastType"] = "success";

                return Redirect(Request.Headers["Referer"].ToString());
            }

            TempData["ToastMessage"] = "Invalid email or password.";
            TempData["ToastType"] = "error";

            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("ClientName");
            HttpContext.Session.Remove("ClientId");
            return RedirectToAction("HomeIndex", "Home");
        }

        #endregion

        #region Contact
        public IActionResult Contact()
        {

            return View();
        }
        #endregion

        #region ProductsPage--shop
        public IActionResult Products(int id)
        {
            var items = db.items
                .Where(p => p.CategoryId == id)
                .Select(p => new ItemsVM
                {
                    ItemId = p.ItemId,
                    ItemName = p.ItemName,
                    CategoryId = p.CategoryId,
                    RecentUnitPrice = p.RecentUnitPrice,
                    Discount = p.Discount,
                    Remarks = p.Remarks
                })
                .ToList();

            if (items.Count == 0)
            {
                return NotFound();
            }

            return View(items);
        }
        #endregion

        #region About
        public IActionResult ABout()
        {
            return View();
        }
        #endregion

        #region Shop
        public IActionResult Shop()
        {
            var categories = db.categories.ToList();
            return View(categories); 
        }

        #endregion


        #region Items
        [HttpGet]
        public IActionResult Items(int id)
        {
            var items = db.items.Where(i => i.CategoryId == id).ToList();
            return View(items);
        }


        #endregion


        #region Cart


        public IActionResult Cart()
        {
            string? clientIdStr = HttpContext.Session.GetString("ClientId");
            string? clientName = HttpContext.Session.GetString("ClientName");

            // Check if client is logged in
            if (string.IsNullOrEmpty(clientIdStr) || string.IsNullOrEmpty(clientName))
            {
                TempData["ToastMessage"] = "Please log in first to view your cart.";
                TempData["ToastType"] = "error";

                var referer = Request.Headers["Referer"].ToString();
                if (string.IsNullOrEmpty(referer))
                    referer = Url.Action("HomeIndex", "Home");

                return Redirect(referer);
            }

            // Check if cart is empty or not present
            var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
            if (cart == null || !cart.Any())
            {
                TempData["ToastMessage"] = "Your cart is empty. Please add items first.";
                TempData["ToastType"] = "error";

                var referer = Request.Headers["Referer"].ToString();
                if (string.IsNullOrEmpty(referer))
                    referer = Url.Action("HomeIndex", "Home");

                return Redirect(referer);
            }

            // If all checks pass, show cart view
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart([FromBody] int id)
        {
            try
            {
                // Get existing cart or create new one
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart")
                           ?? new List<ItemsVM>();

                // Get or initialize CartCount from session
                int cartCount = HttpContext.Session.GetInt32("CartCount") ?? 0;

                // Find existing item in cart
                var existingItem = cart.FirstOrDefault(x => x.ItemId == id);
                bool alreadyExists = false;

                if (existingItem != null)
                {
                    // If item exists, increase quantity
                    existingItem.Quantity += 1;
                    alreadyExists = true;
                }
                else
                {
                    // Get item from database
                    var item = db.items.FirstOrDefault(x => x.ItemId == id);
                    if (item == null)
                    {
                        return Json(new { success = false, message = "Item not found" });
                    }

                    // Add new item to cart
                    cart.Add(new ItemsVM
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        RecentUnitPrice = item.RecentUnitPrice,
                        Picture = item.Picture,
                        Discount = item.Discount,
                        Quantity = 1
                    });

                    // Only increment CartCount if it's a brand new item
                    cartCount++;
                }

                // Save updated cart and cart count to session
                SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                HttpContext.Session.SetInt32("CartCount", cartCount);

                return Json(new
                {
                    success = true,
                    message = alreadyExists ? "Item quantity increased" : "Item added to cart",
                    cartCount = cartCount,
                    alreadyExists = alreadyExists
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                return Json(new { success = false, message = "Error adding item to cart" });
            }
        }


        [HttpGet]
        public IActionResult GetCartCount()
        {
            try
            {
                int count = HttpContext.Session.GetInt32("CartCount") ?? 0;
                return Json(new { success = true, cartCount = count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart count: {ex.Message}");
                return Json(new { success = false, cartCount = 0 });
            }
        }

        public IActionResult Increase(int id)
            {
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
                if (cart != null)
                {
                    var item = cart.FirstOrDefault(x => x.ItemId == id);
                    if (item != null)
                    {
                        item.Quantity++;
                        SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                    }
                }
                return RedirectToAction("Cart");
            }

            public IActionResult Decrease(int id)
            {
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
                if (cart != null)
                {
                    var item = cart.FirstOrDefault(x => x.ItemId == id);
                    if (item != null && item.Quantity > 1)
                    {
                        item.Quantity--;
                        SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                    }
                }
                return RedirectToAction("Cart");
            }


        public IActionResult DeleteFromCart(int id)
        {
            var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
            if (cart != null)
            {
                cart.RemoveAll(x => x.ItemId == id);
                SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
            }
            return RedirectToAction("Cart");
        }

        //[HttpGet]
        //public IActionResult Checkout()
        //{
        //    try
        //    {
        //        // 1. Retrieve cart from session
        //        var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
        //        if (cart == null || !cart.Any())
        //        {
        //            TempData["ToastMessage"] = "Cart is empty. Add items before checkout.";
        //            TempData["ToastType"] = "error";
        //            var referer = Request.Headers["Referer"].ToString();
        //            if (string.IsNullOrEmpty(referer))
        //                referer = Url.Action("HomeIndex", "Home");

        //            return Redirect(referer);
        //        }

        //        // Get client ID if it exists (optional)
        //        int? clientId = null;
        //        string? clientIdStr = HttpContext.Session.GetString("ClientId");
        //        if (int.TryParse(clientIdStr, out int parsedClientId))
        //        {
        //            clientId = parsedClientId;
        //        }

        //        // 2. Create and save a new sale
        //        var sale = new Sales
        //        {
        //            SaleDate = DateTime.Now,
        //            LastModified = DateTime.Now,
        //            ClientId = clientId,
        //            Status = "Pending",
        //            Payment = cart.Sum(x => (x.RecentUnitPrice - (x.Discount ?? 0)) * x.Quantity),
        //            Cash_Received = 0,
        //            Paid_Back = 0,
        //            Modifier = HttpContext.Session.GetString("UserName") ?? "Anonymous",
        //            TokenNumber = new Random().Next(1000, 9999),
        //            Serving = "COD"
        //        };

        //        db.sales.Add(sale);
        //        db.SaveChanges();

        //        // 3. Get the last inserted SaleId
        //        int saleId = sale.SaleId;

        //        foreach (var item in cart)
        //        {
        //            if (item.ItemId == null)
        //                continue;

        //            var unitPrice = item.RecentUnitPrice ?? 0;
        //            var discount = item.Discount ?? 0;
        //            var netPrice = (unitPrice - discount) * item.Quantity;

        //            var soldItem = new SoldItems
        //            {
        //                SaleId = saleId,
        //                ItemId = item.ItemId.Value,
        //                ItemName = item.ItemName,
        //                Qty = item.Quantity,
        //                UnitPrice = unitPrice,
        //                Discount = discount,
        //                NetPrice = netPrice.ToString()
        //            };

        //            db.soldItems.Add(soldItem);
        //        }

        //        db.SaveChanges();

        //        // Clear the cart
        //        HttpContext.Session.Remove("cart");
        //        HttpContext.Session.Remove("CartCount");

               
        //            TempData["ToastMessage"] = "Your Ordre Has Been Placed, Thank You";
        //        TempData["ToastType"] = "error";
        //        var referer = Request.Headers["Referer"].ToString();
        //        if (string.IsNullOrEmpty(referer))
        //            referer = Url.Action("HomeIndex", "Home");

        //        return Redirect(referer);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Checkout error: {ex.Message}");
        //        return Json(new { success = false, message = "Checkout failed" });
        //    }
        //}


        #endregion
    }
}

    
 

  