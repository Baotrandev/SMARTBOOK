
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class AdminBooksController : Controller
    {
        private bookstore_dbEntities db = new bookstore_dbEntities();

        // DANH SÁCH SÁCH
        public ActionResult Index(string keyword)
        {
            var books = db.books
                .Include("category")
                .Include("publisher")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                books = books.Where(b =>
                    b.book_title.Contains(keyword) ||
                    b.ISBN.Contains(keyword));
            }

            ViewBag.Keyword = keyword;

            return View(
                books.OrderByDescending(b => b.book_id).ToList()
            );
        }

        // NẠP DANH MỤC VÀ NHÀ XUẤT BẢN
        private void LoadLists(
            int? categoryId = null,
            int? publisherId = null)
        {
            ViewBag.CategoryList = new SelectList(
                db.categories.ToList(),
                "category_id",
                "category_name",
                categoryId
            );

            ViewBag.PublisherList = new SelectList(
                db.publishers.ToList(),
                "publisher_id",
                "publisher_name",
                publisherId
            );
        }

        // THÊM SÁCH - GET
        public ActionResult Create()
        {
            LoadLists();

            return View(new book());
        }

        // THÊM SÁCH - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(book model)
        {
            if (string.IsNullOrWhiteSpace(model.book_title))
            {
                ModelState.AddModelError(
                    "book_title",
                    "Vui lòng nhập tên sách."
                );
            }

            if (string.IsNullOrWhiteSpace(model.ISBN))
            {
                ModelState.AddModelError(
                    "ISBN",
                    "Vui lòng nhập ISBN."
                );
            }
            else
            {
                model.ISBN = model.ISBN.Trim();

                bool duplicateISBN = db.books.Any(
                    b => b.ISBN == model.ISBN
                );

                if (duplicateISBN)
                {
                    ModelState.AddModelError(
                        "ISBN",
                        "ISBN đã tồn tại."
                    );
                }
            }

            if (model.original_price < 0 ||
                model.sale_price < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Giá sách không được âm."
                );
            }

            if (model.sale_price > model.original_price)
            {
                ModelState.AddModelError(
                    "sale_price",
                    "Giá bán không được lớn hơn giá gốc."
                );
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Giá trị mặc định
                    if (string.IsNullOrWhiteSpace(model.cover_image_url))
                    {
                        model.cover_image_url =
                            "~/Images/no-image.png";
                    }

                    if (string.IsNullOrWhiteSpace(model.status))
                    {
                        model.status = "Dang ban";
                    }

                    if (string.IsNullOrWhiteSpace(model.language))
                    {
                        model.language = "Tiếng Việt";
                    }

                    if (string.IsNullOrWhiteSpace(model.book_format))
                    {
                        model.book_format = "Bìa mềm";
                    }

                    model.created_at = DateTime.Now;
                    model.updated_at = DateTime.Now;

                    db.books.Add(model);

                    db.SaveChanges();

                    TempData["Success"] =
                        "Thêm sách thành công!";

                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var entityError in
                        ex.EntityValidationErrors)
                    {
                        foreach (var error in
                            entityError.ValidationErrors)
                        {
                            ModelState.AddModelError(
                                error.PropertyName,
                                error.PropertyName + ": " +
                                error.ErrorMessage
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Không thể thêm sách: " + ex.Message
                    );
                }
            }

            LoadLists(
                model.category_id,
                model.publisher_id
            );

            return View(model);
        }

        // SỬA SÁCH - GET
        public ActionResult Edit(int id)
        {
            var book = db.books.Find(id);

            if (book == null)
            {
                return HttpNotFound();
            }

            LoadLists(
                book.category_id,
                book.publisher_id
            );

            return View(book);
        }

        // SỬA SÁCH - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(book model)
        {
            if (string.IsNullOrWhiteSpace(model.book_title))
            {
                ModelState.AddModelError(
                    "book_title",
                    "Vui lòng nhập tên sách."
                );
            }

            if (string.IsNullOrWhiteSpace(model.ISBN))
            {
                ModelState.AddModelError(
                    "ISBN",
                    "Vui lòng nhập ISBN."
                );
            }
            else
            {
                model.ISBN = model.ISBN.Trim();

                bool duplicateISBN = db.books.Any(b =>
                    b.ISBN == model.ISBN &&
                    b.book_id != model.book_id
                );

                if (duplicateISBN)
                {
                    ModelState.AddModelError(
                        "ISBN",
                        "ISBN đã được sách khác sử dụng."
                    );
                }
            }

            if (model.original_price < 0 ||
                model.sale_price < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Giá sách không được âm."
                );
            }

            if (model.sale_price > model.original_price)
            {
                ModelState.AddModelError(
                    "sale_price",
                    "Giá bán không được lớn hơn giá gốc."
                );
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var book = db.books.Find(model.book_id);

                    if (book == null)
                    {
                        return HttpNotFound();
                    }

                    book.book_title = model.book_title;
                    book.ISBN = model.ISBN;
                    book.category_id = model.category_id;
                    book.publisher_id = model.publisher_id;
                    book.description = model.description;
                    book.publication_date = model.publication_date;
                    book.page_count = model.page_count;
                    book.language = model.language;
                    book.book_format = model.book_format;

                    book.cover_image_url =
                        string.IsNullOrWhiteSpace(model.cover_image_url)
                        ? "~/Images/no-image.png"
                        : model.cover_image_url;

                    book.original_price = model.original_price;
                    book.sale_price = model.sale_price;
                    book.status = model.status;
                    book.updated_at = DateTime.Now;

                    db.SaveChanges();

                    TempData["Success"] =
                        "Cập nhật sách thành công!";

                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var entityError in
                        ex.EntityValidationErrors)
                    {
                        foreach (var error in
                            entityError.ValidationErrors)
                        {
                            ModelState.AddModelError(
                                error.PropertyName,
                                error.ErrorMessage
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "Không thể cập nhật sách: " + ex.Message
                    );
                }
            }

            LoadLists(
                model.category_id,
                model.publisher_id
            );

            return View(model);
        }

        // CHI TIẾT SÁCH
        public ActionResult Details(int id)
        {
            var book = db.books
                .Include("category")
                .Include("publisher")
                .FirstOrDefault(b => b.book_id == id);

            if (book == null)
            {
                return HttpNotFound();
            }

            return View(book);
        }

        // NGỪNG KINH DOANH SÁCH
        public ActionResult Delete(int id)
        {
            var book = db.books.Find(id);

            if (book == null)
            {
                return HttpNotFound();
            }

            book.status = "Ngung kinh doanh";
            book.updated_at = DateTime.Now;

            db.SaveChanges();

            TempData["Success"] =
                "Đã ngừng kinh doanh sách.";

            return RedirectToAction("Index");
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
