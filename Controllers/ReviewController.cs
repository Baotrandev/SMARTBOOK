
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class ReviewController : Controller
    {
        private readonly bookstore_dbEntities db =
            new bookstore_dbEntities();

        // ==========================================
        // HIỂN THỊ ĐÁNH GIÁ TẠI TRANG CHI TIẾT SÁCH
        // ==========================================
        [ChildActionOnly]
        public ActionResult _BookReviews(int bookId)
        {
            var reviews = db.Database.SqlQuery<ReviewItemViewModel>(
                @"
                SELECT
                    r.review_id AS ReviewId,
                    c.full_name AS CustomerName,
                    r.rating AS Rating,
                    r.review_title AS ReviewTitle,
                    r.review_content AS ReviewContent,
                    r.review_date AS ReviewDate
                FROM dbo.reviews r
                INNER JOIN dbo.customers c
                    ON r.customer_id = c.customer_id
                WHERE
                    r.book_id = @p0
                    AND r.is_approved = 1
                ORDER BY r.review_date DESC;
                ",
                bookId
            ).ToList();

            var model = new BookReviewsViewModel
            {
                BookId = bookId,
                Reviews = reviews,
                TotalReviews = reviews.Count,

                AverageRating = reviews.Any()
                    ? Math.Round(
                        (decimal)reviews.Average(x => x.Rating), 1)
                    : 0,

                NewReview = new ReviewViewModel
                {
                    BookId = bookId
                }
            };

            return PartialView("_BookReviews", model);
        }

        // ==========================================
        // GỬI ĐÁNH GIÁ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(ReviewViewModel model)
        {
            // Kiểm tra dữ liệu gửi lên
            if (model == null || model.BookId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra đăng nhập
            if (Session["CustomerID"] == null)
            {
                TempData["ReviewMessage"] =
                    "Vui lòng đăng nhập để đánh giá sản phẩm.";

                return RedirectToBook(model.BookId);
            }

            int customerId;

            if (!int.TryParse(
                Session["CustomerID"].ToString(),
                out customerId))
            {
                TempData["ReviewMessage"] =
                    "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.";

                return RedirectToBook(model.BookId);
            }

            // Kiểm tra số sao
            if (model.Rating < 1 || model.Rating > 5)
            {
                TempData["ReviewMessage"] =
                    "Vui lòng chọn số sao từ 1 đến 5.";

                return RedirectToBook(model.BookId);
            }

            // Kiểm tra độ dài dữ liệu
            if (!string.IsNullOrWhiteSpace(model.ReviewTitle)
                && model.ReviewTitle.Trim().Length > 200)
            {
                TempData["ReviewMessage"] =
                    "Tiêu đề đánh giá không được vượt quá 200 ký tự.";

                return RedirectToBook(model.BookId);
            }

            if (!string.IsNullOrWhiteSpace(model.ReviewContent)
                && model.ReviewContent.Trim().Length > 1000)
            {
                TempData["ReviewMessage"] =
                    "Nội dung đánh giá không được vượt quá 1000 ký tự.";

                return RedirectToBook(model.BookId);
            }

            try
            {
                // Kiểm tra sách tồn tại
                bool bookExists = db.books.Any(
                    x => x.book_id == model.BookId);

                if (!bookExists)
                {
                    return HttpNotFound();
                }

                // Kiểm tra khách hàng đã mua và nhận sách
                int? orderId = db.Database.SqlQuery<int?>(
                    @"
                    SELECT TOP 1 o.order_id
                    FROM dbo.orders o
                    INNER JOIN dbo.order_items oi
                        ON o.order_id = oi.order_id
                    WHERE
                        o.customer_id = @p0
                        AND oi.book_id = @p1
                        AND o.order_status = N'Da giao'
                    ORDER BY o.order_date DESC;
                    ",
                    customerId,
                    model.BookId
                ).FirstOrDefault();

                if (!orderId.HasValue)
                {
                    TempData["ReviewMessage"] =
                        "Bạn chỉ có thể đánh giá sách đã mua và nhận hàng.";

                    return RedirectToBook(model.BookId);
                }

                // Kiểm tra đã đánh giá sách trong đơn hàng này chưa
                int existingReview = db.Database.SqlQuery<int>(
                    @"
                    SELECT COUNT(*)
                    FROM dbo.reviews
                    WHERE
                        customer_id = @p0
                        AND book_id = @p1
                        AND order_id = @p2;
                    ",
                    customerId,
                    model.BookId,
                    orderId.Value
                ).FirstOrDefault();

                if (existingReview > 0)
                {
                    TempData["ReviewMessage"] =
                        "Bạn đã đánh giá sản phẩm trong đơn hàng này.";

                    return RedirectToBook(model.BookId);
                }

                // Chuẩn bị dữ liệu
                object reviewTitle =
                    string.IsNullOrWhiteSpace(model.ReviewTitle)
                    ? (object)DBNull.Value
                    : model.ReviewTitle.Trim();

                object reviewContent =
                    string.IsNullOrWhiteSpace(model.ReviewContent)
                    ? (object)DBNull.Value
                    : model.ReviewContent.Trim();

                // Lưu đánh giá vào SQL Server
                db.Database.ExecuteSqlCommand(
                    @"
                    INSERT INTO dbo.reviews
                    (
                        customer_id,
                        book_id,
                        order_id,
                        rating,
                        review_title,
                        review_content,
                        review_date,
                        is_approved
                    )
                    VALUES
                    (
                        @p0,
                        @p1,
                        @p2,
                        @p3,
                        @p4,
                        @p5,
                        GETDATE(),
                        0
                    );
                    ",
                    customerId,
                    model.BookId,
                    orderId.Value,
                    model.Rating,
                    reviewTitle,
                    reviewContent
                );

                TempData["ReviewMessage"] =
                    "Đánh giá đã được gửi thành công và đang chờ duyệt.";

                return RedirectToBook(model.BookId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "LỖI REVIEW SUBMIT: " + ex.ToString());

                TempData["ReviewMessage"] =
                    "Không thể gửi đánh giá. Vui lòng kiểm tra lại thông tin.";

                return RedirectToBook(model.BookId);
            }
        }

        // ==========================================
        // QUAY LẠI TRANG CHI TIẾT SÁCH
        // ==========================================
        private ActionResult RedirectToBook(int bookId)
        {
            return RedirectToAction(
                "Details",
                "Products",
                new { id = bookId }
            );
        }

        // ==========================================
        // GIẢI PHÓNG DATABASE
        // ==========================================
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
