using Fastfood.Data;
using Fastfood.Models;
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
        public IActionResult HomeIndex()
        {
            List<Category> categories = new List<Category>();
            categories = db.categories.ToList();
            return View(categories);
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

    }
}