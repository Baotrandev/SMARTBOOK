
using System;
using System.Linq;
using System.Web.Mvc;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class AdminReviewsController : Controller
    {
        // DANH SÁCH ĐÁNH GIÁ
        public ActionResult Index(string keyword, string status)
        {
            using (var db = new bookstore_dbEntities())
            {
                var query =
                    from r in db.reviews
                    join c in db.customers
                        on r.customer_id equals c.customer_id
                    join b in db.books
                        on r.book_id equals b.book_id
                    select new AdminReviewViewModel
                    {
                        ReviewId = r.review_id,
                        CustomerId = r.customer_id,
                        BookId = r.book_id,
                        OrderId = r.order_id,
                        CustomerName = c.full_name,
                        BookTitle = b.book_title,
                        Rating = r.rating,
                        ReviewTitle = r.review_title,
                        ReviewContent = r.review_content,
                        ReviewDate = r.review_date,
                        IsApproved = r.is_approved
                    };

                // TÌM KIẾM
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim();

                    query = query.Where(x =>
                        x.CustomerName.Contains(keyword) ||
                        x.BookTitle.Contains(keyword) ||
                        (x.ReviewTitle != null &&
                         x.ReviewTitle.Contains(keyword)) ||
                        (x.ReviewContent != null &&
                         x.ReviewContent.Contains(keyword)));
                }

                // LỌC TRẠNG THÁI
                if (status == "approved")
                {
                    query = query.Where(x => x.IsApproved);
                }
                else if (status == "pending")
                {
                    query = query.Where(x => !x.IsApproved);
                }

                var reviews = query
                    .OrderByDescending(x => x.ReviewDate)
                    .ToList();

                ViewBag.Keyword = keyword;
                ViewBag.Status = status;

                ViewBag.TotalReviews = db.reviews.Count();
                ViewBag.ApprovedReviews =
                    db.reviews.Count(x => x.is_approved);
                ViewBag.PendingReviews =
                    db.reviews.Count(x => !x.is_approved);

                return View(reviews);
            }
        }

        // DUYỆT HOẶC ẨN ĐÁNH GIÁ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateApproval(int id, bool approved)
        {
            using (var db = new bookstore_dbEntities())
            {
                var review = db.reviews
                    .FirstOrDefault(x => x.review_id == id);

                if (review == null)
                {
                    TempData["Error"] = "Không tìm thấy đánh giá.";
                    return RedirectToAction("Index");
                }

                review.is_approved = approved;
                db.SaveChanges();

                TempData["Success"] = approved
                    ? "Đã duyệt đánh giá thành công."
                    : "Đã ẩn đánh giá thành công.";
            }

            return RedirectToAction("Index");
        }

        // XÓA ĐÁNH GIÁ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            using (var db = new bookstore_dbEntities())
            {
                var review = db.reviews
                    .FirstOrDefault(x => x.review_id == id);

                if (review == null)
                {
                    TempData["Error"] = "Không tìm thấy đánh giá.";
                    return RedirectToAction("Index");
                }

                db.reviews.Remove(review);
                db.SaveChanges();

                TempData["Success"] = "Đã xóa đánh giá thành công.";
            }

            return RedirectToAction("Index");
        }
    }
}
