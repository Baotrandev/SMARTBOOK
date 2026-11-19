
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class ProductController : Controller
    {
        private readonly bookstore_dbEntities db =
            new bookstore_dbEntities();

        public ActionResult Index(int? categoryId, string keyword)
        {
            // lấy ds sách
            var query = db.books
                .Include("category")
                .Include("publisher")
                .AsQueryable();

            // search theo tên sách hoặc isbn
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.book_title.Contains(keyword) ||
                    x.ISBN.Contains(keyword));
            }

            // lọc theo thể loại
            if (categoryId.HasValue)
            {
                query = query.Where(x =>
                    x.category_id == categoryId.Value);
            }

            // sort rồi lấy sách
            var books = query
                .OrderBy(x => x.category_id)
                .ThenBy(x => x.book_title)
                .ToList();

            // lấy tên tác giả từ sql
            var authorRows =
                (from ba in db.book_authors
                 join a in db.authors
                     on ba.author_id equals a.author_id
                 select new
                 {
                     ba.book_id,
                     a.author_name
                 }).ToList();

            // gom tác giả theo từng sách
            var authorsByBook = authorRows
                .GroupBy(x => x.book_id)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ",
                        g.Select(x => x.author_name)
                         .Where(name =>
                             !string.IsNullOrWhiteSpace(name))
                         .Distinct())
                );

            // đẩy tác giả qua view
            ViewBag.AuthorsByBook = authorsByBook;

            // đẩy danh mục qua view
            ViewBag.Categories = db.categories
                .OrderBy(x => x.category_name)
                .ToList();

            ViewBag.SelectedCategory = categoryId;
            ViewBag.Keyword = keyword;
            ViewBag.Categories = db.categories
                   .Where(x => x.is_active == true)
                   .OrderBy(x => x.parent_category_id)
                   .ThenBy(x => x.category_name)
                   .ToList();

            ViewBag.SelectedCategory = categoryId;
            return View(books);
        }
    }
}
