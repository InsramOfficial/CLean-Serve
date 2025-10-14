using AspNetCore.Reporting;
using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using Fastfood.Data;
using Fastfood.Models;
using Fastfood.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using QuestPDF.Fluent;
using Fastfood.Documents;
using QuestPDF.Infrastructure;

namespace Fastfood.Controllers
{
    public class SalesController : Controller
    {
        private readonly IWebHostEnvironment env;
        private readonly DataDbContext db;

        public SalesController(DataDbContext _db, IWebHostEnvironment _env)
        {
            db = _db;
            env = _env;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public IActionResult AccessDenied()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableStockRecords()
        {
            var stock = await db.StockTracking
                .GroupBy(st => new { st.ItemId, st.ItemName })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.ItemName,
                    AvailableQty = g.Sum(x => x.Source != null && x.Source.StartsWith("Purchase") ? x.Qty : -x.Qty)
                })
                .Where(x => x.AvailableQty > 0)
                .ToListAsync();

            return Json(stock);
        }

        public IActionResult SalesIndex()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "SalesIndex";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                                                   .Select(u => u.View)
                                                   .FirstOrDefault();
                if (permission)
                {
                    var clients = db.clients.ToList();
                    var categories = db.categories.ToList();

                    CategoryItemVM categoryItemViewModel = new CategoryItemVM
                    {
                        category = categories,
                        DynamicData = new List<SaledItems>(),
                        clients = clients
                    };

                    TempData["Permission"] = "";
                    return View(categoryItemViewModel);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction("Login", "ControlPanel");
            }
        }

        public IActionResult FilterItems(int categoryId)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            var data = db.items.Where(item => item.CategoryId == categoryId).ToList();
            return Json(data);
        }

        public IActionResult FilterItemsByItemId(int itemId)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            var filteredItems = db.items.Where(item => item.ItemId == itemId).ToList();
            return Json(filteredItems);
        }

        public IActionResult FilterItemsByRemarks(string remarks)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            var filteredPizzaRemarks = db.items.Where(item => item.Remarks == remarks).ToList();
            return Json(filteredPizzaRemarks);
        }

        [HttpPost]
        public IActionResult DeleteSale(int id)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction("Login", "ControlPanel");

            var methodName = "DeleteSale";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                                               .Select(u => u.View)
                                               .FirstOrDefault();

            if (!permission)
                return RedirectToAction(nameof(AccessDenied));

            var sale = db.sales.FirstOrDefault(s => s.SaleId == id);
            if (sale == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Sale not found.";
                return RedirectToAction(nameof(SalesIndex));
            }

            var soldItems = db.soldItems.Where(si => si.SaleId == id).ToList();

            // Restore stock for beverages (create Purchase-like +Qty entries to reverse)
            foreach (var item in soldItems)
            {
                var dbItem = db.items.FirstOrDefault(i => i.ItemId == item.ItemId);
                if (dbItem != null && dbItem.ItemType == "Beverages")
                {
                    db.StockTracking.Add(new StockTracking
                    {
                        TrsID = sale.SaleId,
                        TrsDate = DateTime.Now,
                        ItemId = item.ItemId,
                        Qty = item.Qty,
                        Source = "Sale-Delete",
                        Price = item.UnitPrice,
                        ItemName = item.ItemName,
                        UnitId = db.items.Where(it => it.ItemId == item.ItemId).Select(it => it.CategoryId).FirstOrDefault() // optional fallback; change if your item has UnitId
                    });
                }
            }

            // remove sold items and sale
            db.soldItems.RemoveRange(soldItems);
            db.sales.Remove(sale);
            db.SaveChanges();

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Sale has been deleted successfully.";
            return RedirectToAction(nameof(BillsHistory));
        }

        [HttpPost]
        public IActionResult SaveBillDetail(CategoryItemVM items)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            var username = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") != "true")
                return RedirectToAction("Login", "ControlPanel");

            // permission
            var methodName = "SaveBillDetail";
            var usercode = HttpContext.Session.GetString("UserCode");

            bool permission = db.userPermissions
                .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                .Select(u => u.View)
                .FirstOrDefault();

            if (!permission)
                return RedirectToAction(nameof(AccessDenied));

            // ✅ TOKEN LOGIC START
            // ✅ TOKEN LOGIC START
            DateTime today = DateTime.Today;
            int nextTokenNumber = 1;

            // ✅ Safer query using Date property for time-safe comparison
            var lastSaleToday = db.sales
       .Where(s => s.TokenDate.HasValue && s.TokenDate.Value.Date == today.Date)
       .OrderByDescending(s => s.TokenNumber)
       .FirstOrDefault();


            if (lastSaleToday != null)
            {
                nextTokenNumber = (lastSaleToday.TokenNumber ?? 0) + 1; // ✅ FIX HERE
            }
            // ✅ TOKEN LOGIC END

            // ✅ TOKEN LOGIC END

            // --- Save main Sale record ---
            var sale = new Sales
            {
                SaleDate = DateTime.Now,
                Payment = items.FinalBillTotal,
                Status = items.PaymentMethod,
                Cash_Received = items.CashReceived,
                Paid_Back = items.CashPayBack,
                Serving = items.DeliveryMethod,
                Modifier = username,
                LastModified = DateTime.Now,
                DealingPerson = username,
                ClientId = items.ClientId,

                // ✅ TOKEN FIELDS
                TokenNumber = nextTokenNumber,
                TokenDate = today
            };

            db.sales.Add(sale);
            db.SaveChanges();

            int lastRecordId = sale.SaleId;

            // --- Save sold items and deduct stock ---
            foreach (var item in items.DynamicData)
            {
                var soldItem = new SoldItems
                {
                    SaleId = lastRecordId,
                    ItemId = int.Parse(item.ItemId),
                    ItemName = item.ItemName,
                    Qty = int.Parse(item.Quantity),
                    UnitPrice = int.Parse(item.Price),
                    Discount = int.Parse(item.Discount),
                    NetPrice = item.NetTotal
                };

                db.soldItems.Add(soldItem);

                // Deduct finished product itself (if tracked as inventory)
                var dbItem = db.items.FirstOrDefault(i => i.ItemId == soldItem.ItemId);
                if (dbItem != null && dbItem.ItemType == "Beverages")
                {
                    db.StockTracking.Add(new StockTracking
                    {
                        TrsID = sale.SaleId,
                        TrsDate = sale.SaleDate,
                        ItemId = soldItem.ItemId,
                        Qty = soldItem.Qty,
                        Source = "Sale",
                        Price = soldItem.UnitPrice,
                        ItemName = soldItem.ItemName,
                        UnitId = db.items.Where(it => it.ItemId == soldItem.ItemId)
                                         .Select(it => it.CategoryId)
                                         .FirstOrDefault()
                    });
                }

                // Deduct raw materials (ingredients)
                DeductIngredientsForProduct(soldItem.ItemId, soldItem.Qty, sale.SaleId);
            }

            db.SaveChanges();

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = "Order has been added successfully.";
            return RedirectToAction(nameof(SalesIndex));
        }


        // Convert units helper (expand as needed)
        public decimal ConvertUnits(decimal qty, string fromUnit, string toUnit)
        {
            if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
                return qty;

            // weight conversions
            if (fromUnit.Equals("KG", StringComparison.OrdinalIgnoreCase) &&
                toUnit.Equals("Grams", StringComparison.OrdinalIgnoreCase))
                return qty * 1000m;

            if (fromUnit.Equals("Grams", StringComparison.OrdinalIgnoreCase) &&
                toUnit.Equals("KG", StringComparison.OrdinalIgnoreCase))
                return qty / 1000m;

            // volume conversions
            if (fromUnit.Equals("Liter", StringComparison.OrdinalIgnoreCase) &&
                toUnit.Equals("ml", StringComparison.OrdinalIgnoreCase))
                return qty * 1000m;

            if (fromUnit.Equals("ml", StringComparison.OrdinalIgnoreCase) &&
                toUnit.Equals("Liter", StringComparison.OrdinalIgnoreCase))
                return qty / 1000m;

            // default fallback (1:1)
            return qty;
        }

        private void DeductIngredientsForProduct(int productId, int quantitySold, int saleId)
        {
            var recipeItems = db.RawMaterial_Items_Consumption
                .Where(r => r.BInv_ItemId == productId)
                .ToList();

            foreach (var recipe in recipeItems)
            {
                if (recipe.RMInv_ItemId == null || recipe.RMQTY <= 0)
                    continue;

                decimal totalBaseConsumption = recipe.RMQTY * quantitySold; // e.g. 4 burgers * 50g = 200g

                var raw = db.Consumeables.FirstOrDefault(c => c.CMID == recipe.RMInv_ItemId);
                if (raw == null) continue;

                decimal packWeight = raw.PackWeight ?? 0;
                if (packWeight <= 0) continue; // Prevent wrong calculations

                // ✅ Unit Conversion Logic
                decimal adjustedUsage = ConvertToBaseUnit(totalBaseConsumption, raw.UnitId);

                // ✅ Convert to packs to deduct
                decimal packsToDeduct = adjustedUsage / packWeight;
                if (packsToDeduct <= 0) continue;

                // ✅ Insert into StockTracking (deduct)
                var stockEntry = new StockTracking
                {
                    TrsID = saleId,
                    TrsDate = DateTime.Now,
                    ItemId = raw.CMID,
                    Qty = packsToDeduct,
                    UnitId = raw.UnitId,
                    Source = "Consumption",
                    ItemName = recipe.RMItemName ?? raw.CMName,
                };

                db.StockTracking.Add(stockEntry);
            }

            db.SaveChanges();
        }

        // ✅ Universal Conversion Function
        private decimal ConvertToBaseUnit(decimal value, int? unitId)
        {
            switch (unitId)
            {
                case 1: // Kilogram → Grams
                    return value * 1000;
                case 2: // Grams
                    return value;
                case 5: // Litre → ml
                    return value * 1000;
                case 23: // ml
                    return value;
                case 4: // Dozen → Pieces
                    return value * 12;
                case 6: // Numbers (Nos)
                case 7: // Pieces (PCS)
                case 8: // Packets (PKT)
                case 16: // Pack
                    return value; // Direct piece deduction
                default:
                    return value; // Fallback - no conversion
            }
        }





        // Try to infer unit code from a name like "Chicken 100gm" or "Sugar 1kg"
        private string InferUnitCodeFromName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var m = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)\s*(g|gm|kg|kg\.)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string unit = m.Groups[2].Value.ToLower();
                if (unit.StartsWith("g")) return "Grams";
                if (unit.StartsWith("kg")) return "KG";
            }

            // check ml
            m = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)\s*(ml|milliliter)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return "ml";

            // fallback: try to find a UnitPrice entry that matches a word in name
            var allUnits = db.UnitPrices.ToList();
            foreach (var u in allUnits)
            {
                if (!string.IsNullOrWhiteSpace(u.UnitCode) && name.IndexOf(u.UnitCode, StringComparison.OrdinalIgnoreCase) >= 0)
                    return u.UnitCode;
                if (!string.IsNullOrWhiteSpace(u.UnitName) && name.IndexOf(u.UnitName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return u.UnitCode;
            }

            return null;
        }

        // ... rest of your controller methods (BankDetail, CreateCustomer, BillsHistory, UpdateBill, Print, etc.)
        // (unchanged — omitted here for brevity)
 


        [HttpGet]
        public IActionResult BankDetail(int Bin)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "BankDetail";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
                if (permission)
                {
                    var banksattlement = db.bankSattlements;

                    var specificbankdetail = banksattlement.Where(x => x.BIN == Bin).FirstOrDefault();

                    return Json(specificbankdetail);
                }
                else
                {
                    return RedirectToAction(nameof(AccessDenied));
                }
            }
            else
            {
                return RedirectToAction("Login", "ControlPanel");
            }



        }
        [HttpPost]
        public IActionResult CreateCustomer(string CustomerName)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "CreateCustomer";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
                if (permission)
                {
                    decimal lastRecordId = db.clients
                                         .OrderBy(e => e.Clientid)
                                         .Select(e => e.Clientid)
                                         .LastOrDefault();
                    Client newcustomer = new();
                    newcustomer.Clientid = lastRecordId + 1;
                    newcustomer.Name = CustomerName;
                    db.clients.Add(newcustomer);
                    db.SaveChanges();
                    return RedirectToAction(nameof(SalesIndex));
                }
                else
                {
                    return RedirectToAction(nameof(AccessDenied));
                }
            }
            else
            {
                return RedirectToAction("Login", "ControlPanel");
            }



        }

        public IActionResult BillsHistory()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "BillsHistory";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
                if (permission)
                {
                    var last500Records = db.sales
                                    .OrderByDescending(e => e.SaleId)
                                    .Take(500)
                                    .ToList();

                    TempData["Permission"] = "";
                    return View(last500Records);
                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction("Login", "ControlPanel");
            }



        }

        public IActionResult UpdateBill(int Id)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "UpdateBill";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
                if (permission)
                {
                    CategoryItemVM catitem = new();
                    var categories = db.categories.ToList();
                    List<Category> list = new List<Category>();
                    List<SaledItems> DynamicData = new List<SaledItems>();
                    list = categories;
                    catitem.category = list;
                    //catitem.DynamicData = DynamicData;

                    var sales = db.sales.FirstOrDefault(r => r.SaleId == Id);
                    if (sales != null)
                    {
                        catitem.PaymentMethod = sales.Status;
                        catitem.DeliveryMethod = sales.Serving;
                        catitem.FinalBillTotal = sales.Payment;
                        catitem.CashReceived = sales.Cash_Received;
                        catitem.CashPayBack = sales.Paid_Back;
                        catitem.ClientId = sales.ClientId ?? null;


                    }

                    var solditems = db.soldItems.Where(e => e.SaleId == Id).ToList();

                    foreach (var item in solditems)
                    {
                        SaledItems sold = new();
                        sold.ItemId = item.ItemId.ToString();
                        sold.ItemName = item.ItemName;
                        sold.Price = item.UnitPrice.ToString();
                        sold.Quantity = item.Qty.ToString();
                        sold.Discount = item.Discount.ToString();
                        sold.NetTotal = item.NetPrice;

                        DynamicData.Add(sold);
                    }

                    catitem.DynamicData = DynamicData;
                    catitem.IDforUpdateRecord = Id;
                    TempData["Permission"] = "";
                    return View(catitem);


                }
                else
                {
                    TempData["Permission"] = "You do not have permission to access this page";
                    return View();
                }
            }
            else
            {
                return RedirectToAction("Login", "ControlPanel");
            }
             
        }
        [HttpPost]
        public IActionResult UpdateBill(CategoryItemVM editbill)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            var username = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var recordId = editbill.IDforUpdateRecord;

                // 🔹 Fetch existing sale
                var sale = db.sales.FirstOrDefault(s => s.SaleId == recordId);
                if (sale != null)
                {
                    // --- Main Sale Fields ---
                    sale.Payment = editbill.FinalBillTotal;
                    sale.Status = editbill.PaymentMethod;
                    sale.Cash_Received = editbill.CashReceived;
                    sale.Paid_Back = editbill.CashPayBack;
                    sale.Serving = editbill.DeliveryMethod;
                    sale.SaleDate = DateTime.Now;

                    // --- Audit & user fields ---
                    sale.LastModified = DateTime.Now;
                    sale.Modifier = username;
                    sale.DealingPerson = username;

                    // (Optional) If you want to allow changing SaleDate too
                    // sale.SaleDate = DateTime.Now;  // or from editbill if you add that field

                    db.sales.Update(sale);
                    db.SaveChanges();
                }

                // --- Reset sold items ---
                var recordstodelete = db.soldItems.Where(e => e.SaleId == recordId).ToList();
                db.soldItems.RemoveRange(recordstodelete);
                db.SaveChanges();

                // --- Insert updated sold items ---
                foreach (var item in editbill.DynamicData)
                {
                    SoldItems saleditem = new()
                    {
                        SaleId = (int)recordId,
                        ItemId = int.Parse(item.ItemId),
                        ItemName = item.ItemName,
                        Qty = int.Parse(item.Quantity),
                        UnitPrice = int.Parse(item.Price),
                        Discount = int.Parse(item.Discount),
                        NetPrice = item.NetTotal
                    };
                    db.soldItems.Add(saleditem);

                    // 🔹 If you want same stock update logic as in SaveBillDetail:
                    var dbItem = db.items.FirstOrDefault(i => i.ItemId == saleditem.ItemId);
                    if (dbItem != null && dbItem.ItemType == "Beverages")
                    {
                        var stock = new StockTracking
                        {
                            TrsID = sale.SaleId,
                            TrsDate = DateTime.Now,
                            ItemId = saleditem.ItemId,
                            Qty = saleditem.Qty,
                            Source = "Sale-Update",
                            Price = saleditem.UnitPrice,
                            ItemName = saleditem.ItemName
                        };

                        db.StockTracking.Add(stock);
                    }
                }

                db.SaveChanges();

                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Order has been updated successfully.";
                return RedirectToAction(nameof(BillsHistory));
            }
            else
            {
                return RedirectToAction("Login", "ControlPanel");
            }
        }

        public async Task<IActionResult> Print(int Id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sale = await db.sales.FirstOrDefaultAsync(s => s.SaleId == Id);
            if (sale == null)
                return NotFound("Sale record not found.");

            // ✅ Fetch Client name
            string customerName = "Walk-in Customer";
            if (sale.ClientId != null)
            {
                var client = await db.clients.FirstOrDefaultAsync(c => c.Clientid == sale.ClientId);
                if (client != null)
                    customerName = client.Name;
            }

            // ✅ Fetch Items related to this Sale
            var items = db.soldItems
                .Where(i => i.SaleId == Id)
                .Select(i => new
                {
                    ItemName = i.ItemName,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice
                })
                .ToList<dynamic>();

            // ✅ Calculate change
            double totalAmount = sale.Payment ?? 0;
            double cashReceived = sale.Cash_Received ?? 0;
            double changeBack = sale.Paid_Back ?? (cashReceived - totalAmount);

            string logoPath = Path.Combine(env.WebRootPath, "images", "logo.png");

            // ✅ Pass values into PDF Model
            var document = new InvoiceDocument
            {
                InvoiceNo = sale.SaleId.ToString(),
                DealingPerson = sale.DealingPerson,
                CustomerName = customerName,
                Serving = sale.Serving,
                TotalAmount = totalAmount,
                CashReceived = cashReceived,
                ChangeBack = changeBack,
                Items = items,
                LogoPath = System.IO.File.Exists(logoPath) ? logoPath : ""
            };

            // ✅ Generate PDF and Stream to Browser
            var pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Invoice_{sale.SaleId}.pdf");
        }


    }
}
