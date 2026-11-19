
using SmartBook.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace SmartBook.Controllers
{
    public class BooksController : Controller
    {
        private bookstore_dbEntities db = new bookstore_dbEntities();

        // ds sách + search + lọc
        public ActionResult Index(
            string search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice)
        {
            var query = db.books
                .Include(b => b.category)
                .AsQueryable();

            // tìm theo tên sách hoặc tác giả
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                var bookIds = db.book_authors
                    .Where(ba => db.authors.Any(a =>
                        a.author_id == ba.author_id &&
                        a.author_name.Contains(keyword)))
                    .Select(ba => ba.book_id);

                query = query.Where(b =>
                    b.book_title.Contains(keyword) ||
                    bookIds.Contains(b.book_id));
            }

            // lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(b =>
                    b.category_id == categoryId.Value);
            }

            // lọc giá từ
            if (minPrice.HasValue)
            {
                query = query.Where(b =>
                    b.sale_price >= minPrice.Value);
            }

            // lọc giá tới
            if (maxPrice.HasValue)
            {
                query = query.Where(b =>
                    b.sale_price <= maxPrice.Value);
            }

            ViewBag.Search = search;
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name", categoryId);

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(query.OrderBy(b => b.book_title).ToList());
        }

        // xem chi tiết sách
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            book product = db.books
                .Include("category")
                .Include("publisher")
                .FirstOrDefault(x => x.book_id == id.Value);

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // form thêm sách
        public ActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        // lưu sách mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(book book)
        {
            if (ModelState.IsValid)
            {
                book.created_at = DateTime.Now;
                book.updated_at = null;

                db.books.Add(book);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            LoadDropdowns(book.category_id, book.publisher_id);
            return View(book);
        }

        // form sửa sách
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            book book = db.books.Find(id);

            if (book == null)
                return HttpNotFound();

            LoadDropdowns(book.category_id, book.publisher_id);
            return View(book);
        }

        // lưu sửa sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(book model)
        {
            if (ModelState.IsValid)
            {
                var book = db.books.Find(model.book_id);

                if (book == null)
                    return HttpNotFound();

                book.category_id = model.category_id;
                book.publisher_id = model.publisher_id;
                book.ISBN = model.ISBN;
                book.book_title = model.book_title;
                book.description = model.description;
                book.publication_date = model.publication_date;
                book.page_count = model.page_count;
                book.language = model.language;
                book.book_format = model.book_format;
                book.cover_image_url = model.cover_image_url;
                book.original_price = model.original_price;
                book.sale_price = model.sale_price;
                book.status = model.status;
                book.updated_at = DateTime.Now;

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            LoadDropdowns(model.category_id, model.publisher_id);
            return View(model);
        }

        // xem lại trc khi xóa
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var book = db.books
                .Include(b => b.category)
                .FirstOrDefault(b => b.book_id == id);

            if (book == null)
                return HttpNotFound();

            return View(book);
        }

        // xóa sách
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var book = db.books.Find(id);

            if (book == null)
                return HttpNotFound();

            // sách đã có đơn/giỏ thì k cho xóa
            bool hasOrder = db.order_items.Any(x => x.book_id == id);
            bool hasCart = db.cart_items.Any(x => x.book_id == id);
            bool hasReview = db.reviews.Any(x => x.book_id == id);

            if (hasOrder || hasCart || hasReview)
            {
                TempData["Error"] =
                    "Không thể xóa sách đã có trong giỏ hàng, đơn hàng hoặc đánh giá.";

                return RedirectToAction("Index");
            }

            // xóa data liên quan trc
            var links = db.book_authors.Where(x => x.book_id == id);
            db.book_authors.RemoveRange(links);

            // xóa luôn tồn kho của sách
            var inventoryItem = db.inventories
                .FirstOrDefault(x => x.book_id == id);

            if (inventoryItem != null)
            {
                db.inventories.Remove(inventoryItem);
            }

            db.books.Remove(book);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // load dropdown danh mục + nxb
        private void LoadDropdowns(
            int? categoryId = null,
            int? publisherId = null)
        {
            ViewBag.category_id = new SelectList(db.categories, "category_id", "category_name", categoryId);

            ViewBag.publisher_id = new SelectList(
                db.publishers,
                "publisher_id",
                "publisher_name",
                publisherId
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
