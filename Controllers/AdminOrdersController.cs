
using System;
using System.Linq;
using System.Web.Mvc;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class AdminOrdersController : Controller
    {
        private bookstore_dbEntities db = new bookstore_dbEntities();

        // DANH SÁCH ĐƠN HÀNG
        public ActionResult Index(string keyword, string status)
        {
            if (Session["AdminID"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var query = from o in db.orders
                        join c in db.customers
                        on o.customer_id equals c.customer_id
                        select new AdminOrderViewModel
                        {
                            OrderId = o.order_id,
                            OrderCode = o.order_code,
                            CustomerName = c.full_name,
                            OrderDate = o.order_date,
                            TotalAmount = o.total_amount,
                            OrderStatus = o.order_status,
                            PaymentStatus = o.payment_status
                        };

            // TÌM KIẾM
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.OrderCode.Contains(keyword) ||
                    x.CustomerName.Contains(keyword));
            }

            // LỌC TRẠNG THÁI ĐƠN HÀNG
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.OrderStatus == status);
            }

            // DANH SÁCH TRẠNG THÁI ĐỂ LỌC
            ViewBag.StatusList = db.orders
                .Select(x => x.order_status)
                .Distinct()
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;

            var orders = query
                .OrderByDescending(x => x.OrderDate)
                .ToList();

            return View(orders);
        }

        // CHI TIẾT ĐƠN HÀNG
        public ActionResult Details(int id)
        {
            if (Session["AdminID"] == null)
            {
                return RedirectToAction("Login", "Admin");
            }

            var order = (from o in db.orders
                         join c in db.customers
                         on o.customer_id equals c.customer_id
                         where o.order_id == id
                         select new AdminOrderViewModel
                         {
                             OrderId = o.order_id,
                             OrderCode = o.order_code,
                             CustomerName = c.full_name,
                             OrderDate = o.order_date,
                             TotalAmount = o.total_amount,
                             OrderStatus = o.order_status,
                             PaymentStatus = o.payment_status
                         }).FirstOrDefault();

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
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
