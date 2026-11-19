
using System;
using System.Linq;
using System.Web.Mvc;
using SmartBook.Helpers;
using SmartBook.Models;
using SmartBook.ViewModels;

namespace SmartBook.Controllers
{
    public class AdminController : Controller
    {
        private bookstore_dbEntities db = new bookstore_dbEntities();

        private bool IsAdminLoggedIn()
        {
            return Session["AdminID"] != null &&
                   Session["AdminRole"] != null &&
                   string.Equals(
                       Session["AdminRole"].ToString(),
                       "Admin",
                       StringComparison.OrdinalIgnoreCase
                   );
        }

        public ActionResult Login()
        {
            if (IsAdminLoggedIn())
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ thông tin.";

                return View();
            }

            username = username.Trim();

            var user = db.admins.FirstOrDefault(a =>
                a.username == username &&
                a.status == "Hoat dong");

            if (user == null ||
                !string.Equals(
                    user.role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error =
                    "Tài khoản hoặc mật khẩu không chính xác.";

                return View();
            }

            string storedPassword =
                user.password_salt + ":" +
                user.password_hash;

            bool isValid =
                PasswordHelper.VerifyPassword(
                    password,
                    storedPassword
                );

            if (!isValid)
            {
                ViewBag.Error =
                    "Tài khoản hoặc mật khẩu không chính xác.";

                return View();
            }

            Session["AdminID"] = user.admin_id;
            Session["AdminName"] = user.full_name;
            Session["AdminRole"] = user.role;

            return RedirectToAction("Dashboard");
        }

     
        public ActionResult Dashboard()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            var model = new AdminDashboardViewModel();

            // 1. Don hang da thanh toan
            var paidOrders = db.orders.Where(o =>
                o.payment_status == "Đã thanh toán");

            // 2. Tong doanh thu
            model.TotalRevenue =
                paidOrders
                .Select(o => (decimal?)o.total_amount)
                .Sum() ?? 0m;

            // 3. Tong don hang
            model.TotalOrders = db.orders.Count();

            // 4. Tong luot mua sach
            model.TotalPurchases =
                (from oi in db.order_items
                 join o in db.orders
                    on oi.order_id equals o.order_id
                 where o.payment_status == "Đã thanh toán"
                 select (int?)oi.quantity).Sum() ?? 0;

            // 5. Tong luot danh gia
            model.TotalReviews = db.reviews.Count();

            // 6. Danh gia da duyet
            model.ApprovedReviews =
                db.reviews.Count(r => r.is_approved == true);

            // 7. Doanh thu theo thang
            var monthlyData =
                paidOrders
                .GroupBy(o => new
                {
                    Year = o.order_date.Year,
                    Month = o.order_date.Month
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.total_amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            model.MonthlyRevenue = monthlyData
                .Select(x => new MonthlyRevenueViewModel
                {
                    Year = x.Year,
                    Month = x.Month,
                    Revenue = x.Revenue
                })
                .ToList();

            // 8. Don hang moi nhat
            model.RecentOrders =
                (from o in db.orders
                 join c in db.customers
                    on o.customer_id equals c.customer_id
                 orderby o.order_date descending
                 select new RecentOrderViewModel
                 {
                     OrderId = o.order_id,
                     OrderCode = o.order_code,
                     CustomerName = c.full_name,
                     OrderDate = o.order_date,
                     TotalAmount = o.total_amount,
                     OrderStatus = o.order_status,
                     PaymentStatus = o.payment_status
                 })
                .Take(10)
                .ToList();

            ViewBag.AdminName = Session["AdminName"];

            return View(model);
        }

        // GET: Admin/Logout
        public ActionResult Logout()
        {
            Session.Remove("AdminID");
            Session.Remove("AdminName");
            Session.Remove("AdminRole");

            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
