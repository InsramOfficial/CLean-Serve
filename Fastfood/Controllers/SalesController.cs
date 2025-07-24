using AspNetCore.Reporting;
using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using Fastfood.Data;
using Fastfood.Models;
using Fastfood.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        public IActionResult SalesIndex()
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            //var user = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
            {
				var methodName = "SalesIndex";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					var clients = db.clients.ToList();
					var categories = db.categories.ToList();
					List<Category> list = new List<Category>();
					List<SaledItems> DynamicData = new List<SaledItems>();
					List<Client> _clients = new List<Client>();
					_clients = clients;
					list = categories;
					CategoryItemVM categoryItemViewModel = new CategoryItemVM();
					categoryItemViewModel.category = list;
					categoryItemViewModel.DynamicData = DynamicData;
					categoryItemViewModel.clients = _clients;

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
            var _items = db.items;
            IEnumerable<Item> data = _items.Where(item => item.CategoryId == categoryId).ToList();

            return Json(data);
        }

        public IActionResult FilterItemsByItemId(int itemId)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            // Filter items based on the itemId
            var _items = db.items;

            var filteredItems = _items.Where(item => item.ItemId == itemId).ToList();

            // Return filtered items as JSON data
            return Json(filteredItems);
        }

        public IActionResult FilterItemsByRemarks(string remarks)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            // Filter items based on the itemId
            var _items = db.items;
            //var filteredItems = _items.Where(item => item.ItemId == itemId).ToList();
            var filteredPizzaRemarks = _items.Where(item => item.Remarks == remarks).ToList();

            //var filteredItems = 1;
            // Return filtered items as JSON data
            return Json(filteredPizzaRemarks);
        }

        [HttpPost]
        public IActionResult SaveBillDetail(CategoryItemVM items)
        {
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
			if (HttpContext.Session.GetString("flag") == "true")
			{
				
				var methodName = "SaveBillDetail";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					Sales sale = new();
					sale.SaleDate = System.DateTime.Now;
					sale.Payment = items.FinalBillTotal;
					sale.Status = items.PaymentMethod;
					sale.Cash_Received = items.CashReceived;
					sale.Paid_Back = items.CashPayBack;
					sale.Serving = items.DeliveryMethod;
					db.sales.Add(sale);
					db.SaveChanges();
					int lastRecordId = db.sales
											 .OrderBy(e => e.SaleId)
											 .Select(e => e.SaleId)
											 .LastOrDefault();
					foreach (var item in items.DynamicData)
					{
						SoldItems saleditem = new();
						saleditem.SaleId = lastRecordId;
						saleditem.ItemId = int.Parse(item.ItemId);
						saleditem.ItemName = item.ItemName;
						saleditem.Qty = int.Parse(item.Quantity);
						saleditem.UnitPrice = int.Parse(item.Price);
						saleditem.Discount = int.Parse(item.Discount);
						saleditem.NetPrice = item.NetTotal;
						db.soldItems.Add(saleditem);
						db.SaveChanges();
					}
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

			if (HttpContext.Session.GetString("flag") == "true")
			{
				var RecordtoUpdate = editbill.IDforUpdateRecord;

				Sales sale = new();
				sale.SaleId = (int)RecordtoUpdate;
				sale.LastModified = System.DateTime.Now;
				sale.Payment = editbill.FinalBillTotal;
				sale.Status = editbill.PaymentMethod;
				sale.Cash_Received = editbill.CashReceived;
				sale.Paid_Back = editbill.CashPayBack;
				sale.Serving = editbill.DeliveryMethod;
				db.sales.Update(sale);
				db.SaveChanges();

				var recordstodelete = db.soldItems.Where(e => e.SaleId == RecordtoUpdate).ToList();

				db.soldItems.RemoveRange(recordstodelete);
				db.SaveChanges();

				//int lastRecordId = db.sales
				//                         .OrderBy(e => e.SaleId)
				//                         .Select(e => e.SaleId)
				//                         .LastOrDefault();
				foreach (var item in editbill.DynamicData)
				{
					SoldItems saleditem = new();
					saleditem.SaleId = (int)RecordtoUpdate;
					saleditem.ItemId = int.Parse(item.ItemId);
					saleditem.ItemName = item.ItemName;
					saleditem.Qty = int.Parse(item.Quantity);
					saleditem.UnitPrice = int.Parse(item.Price);
					saleditem.Discount = int.Parse(item.Discount);
					saleditem.NetPrice = item.NetTotal;
					db.soldItems.Add(saleditem);
					db.SaveChanges();
				}

				//var lastrecordtoupdate = db.sales.OrderBy(e => e.SaleId).Select(e => e.SaleId).LastOrDefault();
				return RedirectToAction(nameof(BillsHistory));
			}
			else
			{
				return RedirectToAction("Login", "ControlPanel");
			}

			
        }
        public IActionResult Print(int Id)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

			if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "Print";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					var sales = db.soldItems.Where(i => i.SaleId == Id).ToList();
					var InvoiceNo = Id.ToString();


					string mimType = "";
					int extension = 1;
					var path = $"{this.env.WebRootPath}/Reports/Report.rdlc";
					Dictionary<string, string> parameter = new Dictionary<string, string>();
					parameter.Add("Parameter1", "Welcome to the Sales Invoice Receipt");
					parameter.Add("InvoiceNo", InvoiceNo);
					parameter.Add("InvoiceName", "InvoiceNo");
					LocalReport localreport = new LocalReport(path);
					localreport.AddDataSource("DataSet", sales);
					var result = localreport.Execute(RenderType.Pdf, extension, parameter, mimType);
					return File(result.MainStream, "application/pdf");
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
    }
}
