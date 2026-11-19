
using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using SmartBook.Models;
using SmartBook.ViewModels;

namespace SmartBook.Controllers
{
    public class StoreController : Controller
    {
        private readonly bookstore_dbEntities db =
            new bookstore_dbEntities();

        public ActionResult Index(
            int? categoryId,
            string keyword,
            decimal? minPrice,
            decimal? maxPrice,
            int page = 1,
            string sortOrder = "")
        {
            const int pageSize = 6;

            if (page < 1)
                page = 1;

            // lấy sách từ db
            var products = db.books
                .Include(b => b.category)
                .AsQueryable();

            // lọc theo thể loại
            if (categoryId.HasValue)
            {
                products = products.Where(
                    b => b.category_id == categoryId.Value);
            }

            // search theo tên + mô tả
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                products = products.Where(b =>
                    b.book_title.Contains(keyword) ||
                    (b.description != null &&
                     b.description.Contains(keyword)));
            }

            // lọc theo giá
            if (minPrice.HasValue)
            {
                products = products.Where(
                    b => b.sale_price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(
                    b => b.sale_price <= maxPrice.Value);
            }

            // đếm tổng sp
            int totalItems = products.Count();

            int totalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize);

            if (totalPages < 1)
                totalPages = 1;

            if (page > totalPages)
                page = totalPages;

            // sort
            switch ((sortOrder ?? "").ToLower())
            {
                case "price_asc":
                    products = products
                        .OrderBy(b => b.sale_price);
                    break;

                case "price_desc":
                    products = products
                        .OrderByDescending(b => b.sale_price);
                    break;

                default:
                    products = products
                        .OrderByDescending(b => b.book_id);
                    sortOrder = "";
                    break;
            }

            // build data đẩy ra view
            var model = new StoreHomeViewModel
            {
                Categories = db.categories
                    .OrderBy(c => c.category_name)
                    .ToList(),

                Products = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new ProductCardViewModel
                    {
                        ProductID = b.book_id,
                        NamePro = b.book_title,
                        CategoryName = b.category.category_name,

                        // giá gốc vs giá sale
                        OriginalPrice = b.original_price,
                        Price = b.sale_price,

                        // ảnh có sẵn trong db
                        ImagePro = b.cover_image_url,

                        ShortDescription = b.description

                    }).ToList(),

                SelectedCategoryId = categoryId,
                SearchTerm = keyword,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                SortOrder = sortOrder
            };

            ViewBag.Title = "Tất cả sách";

            return View(model);
        }

        public ActionResult AboutStore()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
