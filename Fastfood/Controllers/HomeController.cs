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
        public IActionResult HomeIndex()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

			if (HttpContext.Session.GetString("flag") == "true")
			{
				return View();
			}
			else
			{
				return RedirectToAction("Login", "ControlPanel");
			}
        }
    }
}