using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using Fastfood.Data;
using Fastfood.Models;
using Fastfood.ViewModel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography.X509Certificates;

namespace Fastfood.Controllers
{
	public class ControlPanelController : Controller
	{
		private readonly DataDbContext db;
		public ControlPanelController(DataDbContext _db)
		{
			db = _db;

		}

        #region ErrorMessage
        public IActionResult ErrorMessage()
        {
            return View();
        }

        #endregion

        #region Category
        // GET: ControlPanelController
        [HttpGet]
		public IActionResult CategoryDetails()
		{
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "CategoryDetails";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();

				if (permission)
				{
					TempData["Permission"] = "";
					var categories = db.categories.ToList();
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
                        Category category = new()
                        {
                            CategoryName = newcat.CategoryName,
                            Root = 0
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
                    // Validation failed
                    return View(newcat);
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

		// POST: ControlPanelController/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult UpdateCategory(Category category)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
			{
				try
				{
					Category cat = new();
					cat.CategoryId = category.CategoryId;
					cat.CategoryName = category.CategoryName;
					cat.Root = 0;
					db.categories.Update(cat);
					db.SaveChanges();
                    TempData["ToastType"] = "success";
                    TempData["ToastMessage"] = $"Category {category.CategoryName} updated successfully";

                    return RedirectToAction(nameof(CategoryDetails));
				}
				catch
				{
					return View();
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
					db.categories.Remove(RemoveCategory);
					db.SaveChanges();
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = $"Category {RemoveCategory.CategoryName} Deleted successfully";
                    return RedirectToAction(nameof(CategoryDetails));
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

		public IActionResult ItemsDetail()
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "ItemsDetail";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					var items = db.items.ToList();
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
		public IActionResult CreateItem()
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "CreateItem";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					ItemsVM items = new();
					var category = db.categories.ToList();
					items.category = category;
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
		public IActionResult CreateItem(ItemsVM item)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				Item addItem = new();
				addItem.ItemName = item.ItemName;
				addItem.RecentUnitPrice = item.RecentUnitPrice;
				addItem.Discount = item.Discount;
				addItem.CategoryId = item.CategoryId;
				addItem.Remarks = item.Remarks;
				db.items.Add(addItem);
				db.SaveChanges();
				return RedirectToAction(nameof(ItemsDetail));
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}


		}
		[HttpGet]
		public IActionResult EditItem(int id)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "EditItem";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					var item = db.items.Find(id);
					ItemsVM items = new();
					items.ItemId = id;
					items.ItemName = item.ItemName;
					items.RecentUnitPrice = item.RecentUnitPrice;
					items.Discount = item.Discount;
					items.CategoryId = item.CategoryId;
					items.Remarks = item.Remarks;
					var categories = db.categories.ToList();
					items.category = categories;
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
		public IActionResult EditItem(ItemsVM item)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				Item updateitem = new();
				updateitem.ItemId = (int)item.ItemId;
				updateitem.ItemName = item.ItemName;
				updateitem.RecentUnitPrice = item.RecentUnitPrice;
				updateitem.Discount = item.Discount;
				updateitem.CategoryId = item.CategoryId;
				updateitem.Remarks = item.Remarks;
				db.items.Update(updateitem);
				db.SaveChanges();
				return RedirectToAction(nameof(ItemsDetail));
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}


		}
		public IActionResult DeleteItem(int id)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "DeleteItem";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					var deleteitem = db.items.Find(id);
					db.items.Remove(deleteitem);
					db.SaveChanges();
					return RedirectToAction(nameof(ItemsDetail));
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

		#region Method
		[HttpGet]
		public IActionResult MethodDetail()
		{
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
			{
				var methodName = "MethodDetail";
				var usercode = HttpContext.Session.GetString("UserCode");

				bool permission = db.userPermissions.Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName).Select(u => u.View).FirstOrDefault();
				if (permission)
				{
					TempData["Permission"] = "";
					var methods = db.methods.ToList();
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
		public IActionResult CreateMethod()
		{
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");


            if (HttpContext.Session.GetString("flag") == "true")
			{
				var userCode = HttpContext.Session.GetString("UserCode");

				string methodname = "CreateMethod";

				var permission = db.userPermissions.Where(u => u.UserCode.ToString() == userCode && u.MethodName == methodname).FirstOrDefault();
				if (permission.View)
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
        public IActionResult CreateMethod(Method method)
        {
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            TempData["Access"] = HttpContext.Session.GetString("Access");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                // Step 1: Add the new method to the Methods table
                Method _method = new()
                {
                    MethodName = method.MethodName
                };
                db.methods.Add(_method);
                db.SaveChanges(); // now _method.MethodId is available

                // Step 2: Get all user codes from the Logins table
                var allUserCodes = db.logins.Select(u => u.UserCode).ToList();

                // Step 3: Add a permission entry for each user for the new method
                foreach (var userCode in allUserCodes)
                {
                    UserPermissions newPermission = new()
                    {
                        UserCode = userCode,
                        MethodId = _method.MethodId,
                        MethodName = _method.MethodName,
                        View = false // Or true if needed
                    };

                    db.userPermissions.Add(newPermission);
                }

                // Step 4: Save all permissions
                db.SaveChanges();
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
		public IActionResult UpdateMethod(int Id)
		{
            TempData["Access"] = HttpContext.Session.GetString("Access");
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
            if (HttpContext.Session.GetString("flag") == "true")
			{
				var userCode = HttpContext.Session.GetString("UserCode");

				string methodname = "UpdateMethod";

				var permission = db.userPermissions.Where(u => u.UserCode.ToString() == userCode && u.MethodName == methodname).FirstOrDefault();
				if (permission.View)
				{
					var method = db.methods.Find(Id);
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
        public IActionResult UpdateMethod(Method method)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                // 1. Update the Method table
                Method _method = new()
                {
                    MethodId = method.MethodId,
                    MethodName = method.MethodName
                };
                db.methods.Update(_method);

                // 2. Get matching permissions from UserPermission table
                var matchingPermissions = db.userPermissions
                    .Where(up => up.MethodId == method.MethodId)
                    .ToList();

                // 3. Update the MethodName in those permission records
                foreach (var permission in matchingPermissions)
                {
                    permission.MethodName = method.MethodName;
                }

                // 4. Save all changes
                db.SaveChanges();

                // 5. Success Toast
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = $"Method {method.MethodName} and related permissions updated successfully";

                return RedirectToAction(nameof(MethodDetail));
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }


        public IActionResult DeleteMethod(int Id)
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");

            if (HttpContext.Session.GetString("flag") == "true")
            {
                var methodName = "DeleteMethod";
                var usercode = HttpContext.Session.GetString("UserCode");

                bool permission = db.userPermissions
                    .Where(u => u.UserCode.ToString() == usercode && u.MethodName == methodName)
                    .Select(u => u.View)
                    .FirstOrDefault();

                if (permission)
                {
                    TempData["Permission"] = "";

                    // Find the method
                    var method = db.methods.Find(Id);
                    if (method != null)
                    {
                        // Delete related UserPermissions by MethodId
                        var permissionsToDelete = db.userPermissions
                            .Where(p => p.MethodId == Id)
                            .ToList();

                        db.userPermissions.RemoveRange(permissionsToDelete);

                        // Delete the method
                        db.methods.Remove(method);

                        db.SaveChanges();

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
					return RedirectToAction("HomeIndex", "Home");

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
		public IActionResult UsersDetail()
		{
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



		public IActionResult AssignPermissions(Guid id)
		{
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
			if (HttpContext.Session.GetString("flag") == "true")
			{
				var permission = HttpContext.Session.GetString("Access");
				if (permission.TrimEnd() == "Admin")
				{
					AssignPermissionVM permissionVM = new();
					List<UserPermissions> listper = new List<UserPermissions>();
					var User = db.logins.Find(id);
					var permissions = db.userPermissions.Where(u => u.UserCode == id).ToList();

					foreach (var item in permissions)
					{
						UserPermissions user = new();
						user.PermissionId = item.PermissionId;
						user.UserCode = item.UserCode;
						user.MethodId = item.MethodId;
						user.MethodName = item.MethodName;
						user.View = item.View;

						listper.Add(user);
					}

					permissionVM.permissions = listper;
					permissionVM.user = User;


					TempData["Permission"] = "";
					return View(permissionVM);

				}
				else
				{
					TempData["Permission"] = "Access Denied Only Admin can Access it";
					return View();
				}
			}
			else
			{
				return RedirectToAction(nameof(Login));
			}


		}
		[HttpPost]
		public IActionResult AssignPermissions(AssignPermissionVM permission)
		{
			TempData["UserName"] = HttpContext.Session.GetString("UserName");
			if (HttpContext.Session.GetString("flag") == "true")
			{
				List<UserPermissions> permits = new List<UserPermissions>();
				foreach (var item in permission.permissions)
				{
					UserPermissions permit = new();
					permit.PermissionId = item.PermissionId;
					permit.UserCode = item.UserCode;
					permit.MethodId = item.MethodId;
					permit.MethodName = item.MethodName;
					permit.View = item.View;

					permits.Add(permit);

				}
				db.userPermissions.UpdateRange(permits);
				db.SaveChanges();

				return RedirectToAction(nameof(AssignPermissions));
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
					return RedirectToAction("AccessDenied"); // Optional: create AccessDenied.cshtml
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

        public IActionResult Profile()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            TempData["UserName"] = HttpContext.Session.GetString("UserName");
            var userId = HttpContext.Session.GetString("UserCode");  
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            Guid userGuid = Guid.Parse(userId);
            var user = db.logins.FirstOrDefault(u => u.UserCode == userGuid);
            if (user == null)
                return NotFound();

            return View(user);
        }


        // GET: Show the change password form
        public IActionResult UpdateProfile()
        {
            TempData["Access"] = HttpContext.Session.GetString("Access");
            string userCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(userCode))
                return RedirectToAction("Login");

            Guid guid = Guid.Parse(userCode);
            var user = db.logins.FirstOrDefault(u => u.UserCode == guid);
            if (user == null)
                return NotFound();

            return View(user);
        }



        [HttpPost]
        public IActionResult UpdateProfile(Register register, string currentPassword, string newPassword, string confirmPassword)
        {
            // Get the logged-in user from session
            string userCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(userCode))
                return RedirectToAction("Login");

            Guid guid = Guid.Parse(userCode);
            var user = db.logins.FirstOrDefault(u => u.UserCode == guid);

            if (user == null)
                return NotFound();

            // Step 1: Verify current password
            if (user.Password != currentPassword)
            {
                TempData["Error"] = "Current password is incorrect.";
                TempData["ToastType"] = "error";
                return View(user);
            }

            // ✅ Step 2: Check if new password is same as current one
            if (newPassword == currentPassword)
            {
                TempData["Error"] = "New password cannot be the same as the current password.";
                TempData["ToastType"] = "error";

                return View(user);
            }

            // Step 3: Confirm both new passwords match
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New passwords do not match.";
                TempData["ToastType"] = "error";

                return View(user);
            }

            // Step 4: Update password
            user.Password = newPassword;
            db.SaveChanges();

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
    }
}
