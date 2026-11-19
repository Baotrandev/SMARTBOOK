
using System;
using System.Linq;
using System.Web.Mvc;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly bookstore_dbEntities db =
            new bookstore_dbEntities();

        // GET: /Reviews
        public ActionResult Index()
        {
            var reviews = db.Database.SqlQuery<AllReviewItemViewModel>(
                @"
                SELECT
                    r.review_id AS ReviewId,
                    b.book_id AS BookId,
                    b.book_title AS BookTitle,
                    c.full_name AS CustomerName,
                    r.rating AS Rating,
                    r.review_title AS ReviewTitle,
                    r.review_content AS ReviewContent,
                    r.review_date AS ReviewDate
                FROM dbo.reviews r
                INNER JOIN dbo.books b
                    ON r.book_id = b.book_id
                INNER JOIN dbo.customers c
                    ON r.customer_id = c.customer_id
                WHERE r.is_approved = 1
                ORDER BY r.review_date DESC;
                "
            ).ToList();

            var model = new AllReviewsViewModel
            {
                Reviews = reviews,
                TotalReviews = reviews.Count,

                AverageRating = reviews.Any()
                    ? Math.Round(
                        (decimal)reviews.Average(x => x.Rating), 1)
                    : 0
            };

            return View(model);
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
