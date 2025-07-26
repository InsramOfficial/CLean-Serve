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
                categories = await db.categories
                                     .AsNoTracking()
                                     .ToListAsync();

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
            var cart = HttpContext.Session.GetSessionObjectFromJson<List<ItemsVM>>("cart") ?? new List<ItemsVM>();
            return View();
        }
        

  

            public IActionResult AddToCart(int id)
            {
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart") ?? new List<ItemsVM>();

                var existingIndex = cart.FindIndex(x => x.ItemId == id);
                if (existingIndex != -1)
                {
                    // Already in cart, increase quantity
                    cart[existingIndex].Discount = (cart[existingIndex].Discount ?? 0) + 1;
                }
                else
                {
                    var item = db.items.FirstOrDefault(x => x.ItemId == id);
                    if (item != null)
                    {
                        cart.Add(new ItemsVM
                        {
                            ItemId = item.ItemId,
                            ItemName = item.ItemName,
                            RecentUnitPrice = item.RecentUnitPrice,
                            Picture = item.Picture,
                            Discount = 1
                        });
                    }
                }

                SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                return RedirectToAction("Index");
            }

            public IActionResult Increase(int id)
            {
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
                if (cart != null)
                {
                    var item = cart.FirstOrDefault(x => x.ItemId == id);
                    if (item != null)
                    {
                        item.Discount = (item.Discount ?? 0) + 1;
                        SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                    }
                }
                return RedirectToAction("Index");
            }

            public IActionResult Decrease(int id)
            {
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
                if (cart != null)
                {
                    var item = cart.FirstOrDefault(x => x.ItemId == id);
                    if (item != null && item.Discount > 1)
                    {
                        item.Discount--;
                        SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                    }
                }
                return RedirectToAction("Index");
            }

            public IActionResult Delete(int id)
            {
                var cart = SessionService.GetSessionObjectFromJson<List<ItemsVM>>(HttpContext.Session, "cart");
                if (cart != null)
                {
                    cart.RemoveAll(x => x.ItemId == id);
                    SessionService.SetSessionObjectJson(HttpContext.Session, "cart", cart);
                }
                return RedirectToAction("Index");
            }
        #endregion
    }
}

    
 

  