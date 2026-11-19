
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using SmartBook.Models;
using SmartBook.Helpers;
using SmartBook.ViewModels;

namespace SmartBook.Controllers
{
    public class AccountController : Controller
    {
        private bookstore_dbEntities db = new bookstore_dbEntities();

        private bool IsCustomerLoggedIn()
        {
            return Session["CustomerID"] != null;
        }

        private customer GetCurrentCustomer()
        {
            if (!IsCustomerLoggedIn())
                return null;

            int customerId = Convert.ToInt32(Session["CustomerID"]);

            return db.customers.FirstOrDefault(
                c => c.customer_id == customerId &&
                     c.status == "Hoat dong");
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.email.Trim().ToLower();

            bool exists = db.customers.Any(c => c.email == email);

            if (exists)
            {
                ModelState.AddModelError(
                    "email",
                    "Email này đã được đăng ký.");

                return View(model);
            }

            customer newCustomer = new customer
            {
                full_name = model.full_name.Trim(),
                email = email,
                password_hash = PasswordHelper.HashPassword(model.password),
                phone = model.phone,
                status = "Hoat dong",
                created_at = DateTime.Now
            };

            db.customers.Add(newCustomer);
            db.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập.";

            return RedirectToAction("Login");
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            if (IsCustomerLoggedIn())
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.email.Trim().ToLower();

            var user = db.customers.FirstOrDefault(
                c => c.email == email &&
                     c.status == "Hoat dong");

            if (user == null ||
                !PasswordHelper.VerifyPassword(
                    model.password, user.password_hash))
            {
                ModelState.AddModelError(
                    "",
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            FormsAuthentication.SetAuthCookie(
                user.customer_id.ToString(), false);

            Session["CustomerID"] = user.customer_id;
            Session["CustomerName"] = user.full_name;

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Index - Thông tin cá nhân
        public ActionResult Index()
        {
            var user = GetCurrentCustomer();

            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        // GET: Account/EditProfile
        public ActionResult EditProfile()
        {
            var user = GetCurrentCustomer();

            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(string full_name, string phone)
        {
            var user = GetCurrentCustomer();

            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(full_name))
            {
                ModelState.AddModelError(
                    "full_name",
                    "Vui lòng nhập họ và tên.");

                return View(user);
            }

            user.full_name = full_name.Trim();
            user.phone = string.IsNullOrWhiteSpace(phone)
                ? null
                : phone.Trim();

            db.SaveChanges();

            Session["CustomerName"] = user.full_name;

            TempData["Success"] = "Cập nhật hồ sơ thành công!";

            return RedirectToAction("Index");
        }

        // GET: Account/ChangePassword
        public ActionResult ChangePassword()
        {
            if (GetCurrentCustomer() == null)
                return RedirectToAction("Login");

            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(
            string oldPassword,
            string newPassword,
            string confirmPassword)
        {
            var user = GetCurrentCustomer();

            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (!PasswordHelper.VerifyPassword(
                oldPassword, user.password_hash))
            {
                ViewBag.Error = "Mật khẩu hiện tại không chính xác.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            user.password_hash =
                PasswordHelper.HashPassword(newPassword);

            db.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";

            return RedirectToAction("Index");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            Session.Remove("CustomerID");
            Session.Remove("CustomerName");

            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
