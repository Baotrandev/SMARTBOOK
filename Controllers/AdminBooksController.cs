
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Collections.Generic;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class AdminBooksController : Controller
    {
        private bookstore_dbEntities db = new bookstore_dbEntities();

       

        private const int MaxImageSize = 5 * 1024 * 1024;

        private readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        // Kiểm tra định dạng ảnh
        private bool IsAllowedImage(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            return AllowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase
            );
        }

        // Lưu ảnh tải từ máy tính vào Images/Books
        private string SaveBookImage(HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
            {
                return null;
            }

            if (imageFile.ContentLength > MaxImageSize)
            {
                throw new Exception("Ảnh không được vượt quá 5MB.");
            }

            if (!IsAllowedImage(imageFile.FileName))
            {
                throw new Exception(
                    "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP."
                );
            }

            string folderPath = Server.MapPath("~/Images/Books");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string extension = Path.GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            string fileName = Guid.NewGuid().ToString("N") + extension;

            string fullPath = Path.Combine(folderPath, fileName);

            imageFile.SaveAs(fullPath);

            return "~/Images/Books/" + fileName;
        }

        // Kiểm tra đường dẫn ảnh có thuộc thư mục Images hay không
        private bool IsValidImagePath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return false;
            }

            imagePath = imagePath.Trim();

            if (!imagePath.StartsWith(
                "~/Images/",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!IsAllowedImage(imagePath))
            {
                return false;
            }

            try
            {
                string imagesFolder = Path.GetFullPath(
                    Server.MapPath("~/Images")
                );

                string fullPath = Path.GetFullPath(
                    Server.MapPath(imagePath)
                );

                string folderPrefix = imagesFolder.TrimEnd(
                    Path.DirectorySeparatorChar
                ) + Path.DirectorySeparatorChar;

                return fullPath.StartsWith(
                    folderPrefix,
                    StringComparison.OrdinalIgnoreCase
                ) && System.IO.File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

      

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

        // Lấy ảnh từ Images và các thư mục con, bao gồm Images/Books
        private void LoadImageList(string selectedImage = null)
        {
            string imageFolder = Server.MapPath("~/Images");

            List<SelectListItem> imageOptions =
                new List<SelectListItem>();

            if (Directory.Exists(imageFolder))
            {
                string[] files = Directory.GetFiles(
                    imageFolder,
                    "*.*",
                    SearchOption.AllDirectories
                );

                foreach (string file in files)
                {
                    if (!IsAllowedImage(file))
                    {
                        continue;
                    }

                    string relativePath = "~/" +
                        file.Substring(
                            Server.MapPath("~/").Length
                        ).Replace("\\", "/");

                    imageOptions.Add(new SelectListItem
                    {
                        Text = relativePath,
                        Value = relativePath,
                        Selected = relativePath.Equals(
                            selectedImage,
                            StringComparison.OrdinalIgnoreCase
                        )
                    });
                }
            }

            ViewBag.ImageList = new SelectList(
                imageOptions,
                "Value",
                "Text",
                selectedImage
            );
        }

        
        private void LoadFormData(
            int? categoryId = null,
            int? publisherId = null,
            string selectedImage = null)
        {
            LoadLists(categoryId, publisherId);
            LoadImageList(selectedImage);
        }

        

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
                    b.ISBN.Contains(keyword)
                );
            }

            ViewBag.Keyword = keyword;

            return View(
                books.OrderByDescending(b => b.book_id).ToList()
            );
        }

        

        public ActionResult Create()
        {
            var model = new book
            {
                status = "Dang ban",
                language = "Tiếng Việt",
                book_format = "Bìa mềm",
                cover_image_url = "~/Images/no-image.png"
            };

            LoadFormData(
                null,
                null,
                model.cover_image_url
            );

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            book model,
            HttpPostedFileBase imageFile)
        {
            // Kiểm tra tên sách
            if (string.IsNullOrWhiteSpace(model.book_title))
            {
                ModelState.AddModelError(
                    "book_title",
                    "Vui lòng nhập tên sách."
                );
            }
            else
            {
                model.book_title = model.book_title.Trim();
            }

            // Kiểm tra ISBN
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

                if (db.books.Any(b => b.ISBN == model.ISBN))
                {
                    ModelState.AddModelError(
                        "ISBN",
                        "ISBN đã tồn tại."
                    );
                }
            }

            // Kiểm tra danh mục
            if (!db.categories.Any(
                c => c.category_id == model.category_id))
            {
                ModelState.AddModelError(
                    "category_id",
                    "Vui lòng chọn danh mục hợp lệ."
                );
            }

            // Kiểm tra nhà xuất bản nếu có chọn
            if (model.publisher_id != null &&
                !db.publishers.Any(
                    p => p.publisher_id == model.publisher_id))
            {
                ModelState.AddModelError(
                    "publisher_id",
                    "Nhà xuất bản không hợp lệ."
                );
            }

            // Kiểm tra giá
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

            // Kiểm tra ảnh tải lên
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                if (!IsAllowedImage(imageFile.FileName))
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP."
                    );
                }

                if (imageFile.ContentLength > MaxImageSize)
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Dung lượng ảnh không được vượt quá 5MB."
                    );
                }
            }
            else
            {
                // Nếu không tải ảnh mới thì kiểm tra ảnh đã chọn
                if (string.IsNullOrWhiteSpace(model.cover_image_url))
                {
                    model.cover_image_url = "~/Images/no-image.png";
                }

                if (!IsValidImagePath(model.cover_image_url))
                {
                    ModelState.AddModelError(
                        "cover_image_url",
                        "Vui lòng chọn ảnh hợp lệ trong thư mục Images."
                    );
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Nếu tải ảnh từ máy tính
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        model.cover_image_url = SaveBookImage(imageFile);
                    }

                    // Thiết lập giá trị mặc định
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

                    // Lưu sách vào bảng books
                    db.books.Add(model);

                    db.SaveChanges();

                    TempData["Success"] =
                        "Thêm sách mới thành công!";

                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var entityError in ex.EntityValidationErrors)
                    {
                        foreach (var error in entityError.ValidationErrors)
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
                        "Không thể thêm sách: " + GetErrorMessage(ex)
                    );
                }
            }

            // Có lỗi thì nạp lại danh sách để hiển thị form
            LoadFormData(
                model.category_id,
                model.publisher_id,
                model.cover_image_url
            );

            return View(model);
        }

        // ==========================================
        // SỬA SÁCH - GET
        // ==========================================

        public ActionResult Edit(int id)
        {
            var book = db.books.Find(id);

            if (book == null)
            {
                return HttpNotFound();
            }

            LoadFormData(
                book.category_id,
                book.publisher_id,
                book.cover_image_url
            );

            return View(book);
        }

        // ==========================================
        // SỬA SÁCH - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            book model,
            HttpPostedFileBase imageFile)
        {
            // Kiểm tra sách
            var book = db.books.Find(model.book_id);

            if (book == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra tên sách
            if (string.IsNullOrWhiteSpace(model.book_title))
            {
                ModelState.AddModelError(
                    "book_title",
                    "Vui lòng nhập tên sách."
                );
            }
            else
            {
                model.book_title = model.book_title.Trim();
            }

            // Kiểm tra ISBN
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

                if (db.books.Any(b =>
                    b.ISBN == model.ISBN &&
                    b.book_id != model.book_id))
                {
                    ModelState.AddModelError(
                        "ISBN",
                        "ISBN đã được sách khác sử dụng."
                    );
                }
            }

            // Kiểm tra danh mục
            if (!db.categories.Any(
                c => c.category_id == model.category_id))
            {
                ModelState.AddModelError(
                    "category_id",
                    "Vui lòng chọn danh mục hợp lệ."
                );
            }

            // Kiểm tra nhà xuất bản nếu có chọn
            if (model.publisher_id != null &&
                !db.publishers.Any(
                    p => p.publisher_id == model.publisher_id))
            {
                ModelState.AddModelError(
                    "publisher_id",
                    "Nhà xuất bản không hợp lệ."
                );
            }

            // Kiểm tra giá
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

            // Kiểm tra ảnh tải lên
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                if (!IsAllowedImage(imageFile.FileName))
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Định dạng ảnh không hợp lệ."
                    );
                }

                if (imageFile.ContentLength > MaxImageSize)
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Dung lượng ảnh không được vượt quá 5MB."
                    );
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.cover_image_url))
            {
                // Chỉ kiểm tra khi Admin có chọn ảnh khác
                if (!IsValidImagePath(model.cover_image_url))
                {
                    ModelState.AddModelError(
                        "cover_image_url",
                        "Ảnh bìa không tồn tại trong thư mục Images."
                    );
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật thông tin sách
                    book.book_title = model.book_title;
                    book.ISBN = model.ISBN;
                    book.category_id = model.category_id;
                    book.publisher_id = model.publisher_id;
                    book.description = model.description;
                    book.publication_date = model.publication_date;
                    book.page_count = model.page_count;
                    book.language = model.language;
                    book.book_format = model.book_format;
                    book.original_price = model.original_price;
                    book.sale_price = model.sale_price;
                    book.status = model.status;

                    
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        book.cover_image_url = SaveBookImage(imageFile);
                    }
                    
                    else if (!string.IsNullOrWhiteSpace(model.cover_image_url))
                    {
                        book.cover_image_url = model.cover_image_url;
                    }

                    book.updated_at = DateTime.Now;

                    db.SaveChanges();

                    TempData["Success"] =
                        "Cập nhật thông tin và hình ảnh sách thành công!";

                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var entityError in ex.EntityValidationErrors)
                    {
                        foreach (var error in entityError.ValidationErrors)
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
                        "Không thể cập nhật sách: " + GetErrorMessage(ex)
                    );
                }
            }

            LoadFormData(
                model.category_id,
                model.publisher_id,
                model.cover_image_url ?? book.cover_image_url
            );

            return View(model);
        }

      

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

       

        [HttpPost]
        [ValidateAntiForgeryToken]
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

       
        private string GetErrorMessage(Exception ex)
        {
            string message = ex.Message;

            Exception inner = ex.InnerException;

            while (inner != null)
            {
                message += " | " + inner.Message;
                inner = inner.InnerException;
            }

            return message;
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
