
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SmartBook.Models;

namespace SmartBook.Controllers
{
    public class OrdersController : Controller
    {
        private readonly bookstore_dbEntities db =
            new bookstore_dbEntities();

        private const string CartKey = "SmartBookCart";

        private class PromotionInfo
        {
            public int promotion_id { get; set; }
            public string promotion_code { get; set; }
            public string discount_type { get; set; }
            public decimal discount_value { get; set; }
            public decimal? minimum_order_value { get; set; }
            public decimal? maximum_discount { get; set; }
            public DateTime? start_date { get; set; }
            public DateTime? end_date { get; set; }
            public bool is_active { get; set; }
        }

        // ==========================================
        // ĐƠN HÀNG CỦA TÔI
        // GET: /Orders
        // ==========================================

        [HttpGet]
        public ActionResult Index()
        {
            // Yêu cầu đăng nhập
            if (Session["CustomerID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId;

            if (!int.TryParse(
                Session["CustomerID"].ToString(),
                out customerId) || customerId <= 0)
            {
                Session.Remove("CustomerID");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var orders = db.Database.SqlQuery<OrderHistoryViewModel>(
                    @"
                    SELECT
                        o.order_id AS OrderId,
                        o.order_code AS OrderCode,
                        o.order_date AS OrderDate,
                        o.subtotal AS Subtotal,
                        o.discount_amount AS DiscountAmount,
                        o.shipping_fee AS ShippingFee,
                        o.total_amount AS TotalAmount,
                        o.order_status AS OrderStatus,
                        o.payment_status AS PaymentStatus,
                        o.customer_note AS CustomerNote,

                        CAST(
                            ISNULL(
                                (
                                    SELECT SUM(oi.quantity)
                                    FROM dbo.order_items oi
                                    WHERE oi.order_id = o.order_id
                                ), 0
                            ) AS INT
                        ) AS TotalItems

                    FROM dbo.orders o

                    WHERE o.customer_id = @p0

                    ORDER BY o.order_date DESC;
                    ",
                    customerId
                ).ToList();

                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Lỗi tải lịch sử đơn hàng: " + ex);

                return View(new List<OrderHistoryViewModel>());
            }
        }

        // ==========================================
        // BẢNG PHÍ VẬN CHUYỂN
        // ==========================================

        private decimal GetShippingFee(string partner)
        {
            switch (partner)
            {
                case "SFX Express":
                    return 30000;

                case "JT Express":
                    return 30000;

                case "HoaTocGrab":
                case "GrabExpress":
                    return 25000;

                default:
                    return -1;
            }
        }

        // ==========================================
        // CHUẨN HÓA PHƯƠNG THỨC THANH TOÁN
        // ==========================================

        private string NormalizePaymentMethod(string method)
        {
            if (method == "COD" ||
                method == "Thanh toan khi nhan hang")
            {
                return "Thanh toan khi nhan hang";
            }

            if (method == "OnlineBanking" ||
                method == "Chuyen khoan")
            {
                return "Chuyen khoan";
            }

            return null;
        }

        // ==========================================
        // LẤY THÔNG TIN VOUCHER
        // ==========================================

        private PromotionInfo GetPromotion(string code)
        {
            return db.Database.SqlQuery<PromotionInfo>(
                @"
                SELECT
                    promotion_id,
                    promotion_code,
                    discount_type,
                    discount_value,
                    minimum_order_value,
                    maximum_discount,
                    start_date,
                    end_date,
                    is_active
                FROM dbo.promotions
                WHERE UPPER(promotion_code) = UPPER(@p0)
                ",
                code
            ).FirstOrDefault();
        }

        // ==========================================
        // TÍNH TIỀN ĐƠN HÀNG
        // ==========================================

        private bool CalculateCheckout(
            CheckoutViewModel model,
            List<CartItemViewModel> cart,
            out int? promotionId,
            out string error)
        {
            promotionId = null;
            error = null;

            decimal subtotal = 0;

            // Lấy giá mới nhất từ database
            foreach (var item in cart)
            {
                if (item.Quantity <= 0)
                {
                    error = "Số lượng sách không hợp lệ.";
                    return false;
                }

                var book = db.books.FirstOrDefault(
                    x => x.book_id == item.BookId);

                if (book == null)
                {
                    error = "Một cuốn sách trong giỏ hàng không còn tồn tại.";
                    return false;
                }

                item.BookTitle = book.book_title;
                item.ImageUrl = book.cover_image_url;
                item.UnitPrice = book.sale_price;

                subtotal += item.UnitPrice * item.Quantity;
            }

            decimal shippingFee = GetShippingFee(
                model.ShippingPartner);

            if (shippingFee < 0)
            {
                error = "Vui lòng chọn đơn vị vận chuyển hợp lệ.";
                return false;
            }

            decimal voucherDiscount = 0;
            decimal shippingDiscount = 0;

            // Kiểm tra voucher
            if (!string.IsNullOrWhiteSpace(model.VoucherCode))
            {
                string code = model.VoucherCode.Trim().ToUpper();

                var promotion = GetPromotion(code);

                if (promotion == null)
                {
                    error = "Mã voucher không tồn tại.";
                    return false;
                }

                DateTime now = DateTime.Now;

                if (!promotion.is_active)
                {
                    error = "Voucher hiện không còn hoạt động.";
                    return false;
                }

                if (promotion.start_date.HasValue &&
                    now < promotion.start_date.Value)
                {
                    error = "Voucher chưa đến thời gian sử dụng.";
                    return false;
                }

                if (promotion.end_date.HasValue &&
                    now > promotion.end_date.Value)
                {
                    error = "Voucher đã hết hạn.";
                    return false;
                }

                if (subtotal <
                    (promotion.minimum_order_value ?? 0))
                {
                    error = "Đơn hàng chưa đạt giá trị tối thiểu để sử dụng voucher.";
                    return false;
                }

                // GRAB27 chỉ giảm phí vận chuyển GrabExpress
                if (code == "GRAB27")
                {
                    if (model.ShippingPartner != "HoaTocGrab" &&
                        model.ShippingPartner != "GrabExpress")
                    {
                        error = "Voucher GRAB27 chỉ áp dụng khi chọn GrabExpress.";
                        return false;
                    }

                    shippingDiscount = promotion.discount_value;

                    if (promotion.maximum_discount.HasValue)
                    {
                        shippingDiscount = Math.Min(
                            shippingDiscount,
                            promotion.maximum_discount.Value);
                    }

                    shippingDiscount = Math.Min(
                        shippingDiscount,
                        shippingFee);
                }
                else
                {
                    // Voucher giảm tiền hàng
                    if (promotion.discount_type == "Percent")
                    {
                        voucherDiscount =
                            subtotal * promotion.discount_value / 100;
                    }
                    else if (promotion.discount_type == "Fixed")
                    {
                        voucherDiscount = promotion.discount_value;
                    }
                    else
                    {
                        error = "Loại voucher không hợp lệ.";
                        return false;
                    }

                    if (promotion.maximum_discount.HasValue)
                    {
                        voucherDiscount = Math.Min(
                            voucherDiscount,
                            promotion.maximum_discount.Value);
                    }

                    voucherDiscount = Math.Min(
                        voucherDiscount,
                        subtotal);
                }

                promotionId = promotion.promotion_id;
            }

            decimal totalDiscount =
                voucherDiscount + shippingDiscount;

            decimal grandTotal =
                subtotal + shippingFee - totalDiscount;

            if (grandTotal < 0)
                grandTotal = 0;

            // Đưa kết quả tính toán lên giao diện
            model.Items = cart;
            model.Subtotal = subtotal;
            model.ShippingFee = shippingFee;
            model.VoucherDiscount = voucherDiscount;
            model.ShippingDiscount = shippingDiscount;
            model.GrandTotal = grandTotal;

            return true;
        }

        // ==========================================
        // HIỂN THỊ TRANG CHECKOUT
        // ==========================================

        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = Session[CartKey]
                as List<CartItemViewModel>;

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            var model = new CheckoutViewModel();

            // Mặc định chọn SFX Express và COD
            model.ShippingPartner = "SFX Express";
            model.PaymentMethod = "Thanh toan khi nhan hang";

            // Tự điền thông tin khách hàng
            if (Session["CustomerID"] != null)
            {
                int customerId;

                if (int.TryParse(
                    Session["CustomerID"].ToString(),
                    out customerId))
                {
                    var customer = db.customers.FirstOrDefault(
                        x => x.customer_id == customerId);

                    if (customer != null)
                    {
                        model.FullName = customer.full_name;
                        model.Phone = customer.phone;
                    }
                }
            }

            int? promotionId;
            string error;

            CalculateCheckout(
                model,
                cart,
                out promotionId,
                out error);

            return View(model);
        }

        // ==========================================
        // XỬ LÝ ĐẶT HÀNG
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            var cart = Session[CartKey]
                as List<CartItemViewModel>;

            if (cart == null || !cart.Any())
                return RedirectToAction("Index", "Cart");

            // Chuẩn hóa phương thức thanh toán
            string paymentMethod =
                NormalizePaymentMethod(model.PaymentMethod);

            if (paymentMethod == null)
            {
                ModelState.AddModelError(
                    "PaymentMethod",
                    "Phương thức thanh toán không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                int? tempPromotionId;
                string tempError;

                CalculateCheckout(
                    model,
                    cart,
                    out tempPromotionId,
                    out tempError);

                return View(model);
            }

            // Kiểm tra dữ liệu
            if (string.IsNullOrWhiteSpace(model.FullName) ||
                model.FullName.Trim().Length > 150)
            {
                ModelState.AddModelError(
                    "FullName",
                    "Họ tên không hợp lệ hoặc vượt quá 150 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(model.Phone) ||
                string.IsNullOrWhiteSpace(model.Address) ||
                model.Address.Trim().Length > 255)
            {
                ModelState.AddModelError(
                    "Address",
                    "Vui lòng nhập địa chỉ hợp lệ, tối đa 255 ký tự.");
            }

            if (!string.IsNullOrWhiteSpace(model.Note) &&
                model.Note.Trim().Length > 500)
            {
                ModelState.AddModelError(
                    "Note",
                    "Ghi chú không được vượt quá 500 ký tự.");
            }

            // Tính lại tiền trước khi lưu
            int? promotionId;
            string calculationError;

            bool calculationValid = CalculateCheckout(
                model,
                cart,
                out promotionId,
                out calculationError);

            if (!calculationValid)
            {
                ModelState.AddModelError(
                    "VoucherCode",
                    calculationError);
            }

            if (!ModelState.IsValid)
                return View(model);

            decimal subtotal = model.Subtotal;
            decimal shipping = model.ShippingFee;

            decimal discount =
                model.VoucherDiscount + model.ShippingDiscount;

            decimal total = model.GrandTotal;

            using (var transaction =
                db.Database.BeginTransaction())
            {
                try
                {
                    // B1: Xác định khách hàng
                    int customerId = 0;

                    if (Session["CustomerID"] != null)
                    {
                        int.TryParse(
                            Session["CustomerID"].ToString(),
                            out customerId);
                    }

                    var customer = customerId > 0
                        ? db.customers.FirstOrDefault(
                            x => x.customer_id == customerId)
                        : null;

                    // Tạo khách hàng vãng lai nếu chưa đăng nhập
                    if (customer == null)
                    {
                        string guestEmail =
                            "guest_" +
                            Guid.NewGuid().ToString("N") +
                            "@smartbook.local";

                        string guestPassword =
                            SmartBook.Helpers.PasswordHelper.HashPassword(
                                Guid.NewGuid().ToString("N"));

                        customerId = db.Database.SqlQuery<int>(
                            @"
                            INSERT INTO dbo.customers
                                (full_name, email, password_hash)
                            VALUES
                                (@p0, @p1, @p2);

                            SELECT CAST(SCOPE_IDENTITY() AS int);
                            ",
                            model.FullName.Trim(),
                            guestEmail,
                            guestPassword
                        ).Single();
                    }

                    // B2: Tạo địa chỉ nhận hàng
                    int addressId = db.Database.SqlQuery<int>(
                        @"
                        INSERT INTO dbo.customer_addresses
                            (
                                customer_id,
                                recipient_name,
                                phone,
                                province,
                                district,
                                street_address,
                                is_default
                            )
                        VALUES
                            (
                                @p0,
                                @p1,
                                @p2,
                                @p3,
                                @p4,
                                @p5,
                                0
                            );

                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        ",
                        customerId,
                        model.FullName.Trim(),
                        model.Phone.Trim(),
                        "Chưa cung cấp",
                        "Chưa cung cấp",
                        model.Address.Trim()
                    ).Single();

                    // B3: Kiểm tra và trừ tồn kho
                    foreach (var item in cart)
                    {
                        int affectedRows =
                            db.Database.ExecuteSqlCommand(
                                @"
                                UPDATE dbo.inventory
                                SET
                                    quantity_in_stock =
                                        quantity_in_stock - @p0,
                                    last_stock_update = SYSDATETIME()
                                WHERE
                                    book_id = @p1
                                    AND quantity_in_stock
                                        - quantity_reserved >= @p0;
                                ",
                                item.Quantity,
                                item.BookId
                            );

                        if (affectedRows == 0)
                        {
                            throw new Exception(
                                "Sách '" + item.BookTitle +
                                "' không đủ số lượng tồn kho.");
                        }
                    }

                    // B4: Tạo mã đơn hàng
                    string orderCode =
                        "SB" +
                        DateTime.Now.ToString("yyyyMMddHHmmss") +
                        Guid.NewGuid()
                            .ToString("N")
                            .Substring(0, 4)
                            .ToUpper();

                    // B5: Lưu đơn hàng
                    int orderId = db.Database.SqlQuery<int>(
                        @"
                        INSERT INTO dbo.orders
                            (
                                customer_id,
                                address_id,
                                promotion_id,
                                order_code,
                                order_date,
                                subtotal,
                                discount_amount,
                                shipping_fee,
                                total_amount,
                                order_status,
                                payment_status,
                                customer_note
                            )
                        VALUES
                            (
                                @p0,
                                @p1,
                                @p2,
                                @p3,
                                SYSDATETIME(),
                                @p4,
                                @p5,
                                @p6,
                                @p7,
                                @p8,
                                @p9,
                                @p10
                            );

                        SELECT CAST(SCOPE_IDENTITY() AS int);
                        ",
                        customerId,
                        addressId,
                        promotionId.HasValue
                            ? (object)promotionId.Value
                            : DBNull.Value,
                        orderCode,
                        subtotal,
                        discount,
                        shipping,
                        total,
                        "Cho xac nhan",
                        "Chua thanh toan",
                        string.IsNullOrWhiteSpace(model.Note)
                            ? (object)DBNull.Value
                            : model.Note.Trim()
                    ).Single();

                    // B6: Lưu chi tiết đơn hàng
                    foreach (var item in cart)
                    {
                        decimal lineTotal =
                            item.UnitPrice * item.Quantity;

                        db.Database.ExecuteSqlCommand(
                            @"
                            INSERT INTO dbo.order_items
                                (
                                    order_id,
                                    book_id,
                                    quantity,
                                    unit_price,
                                    discount_amount,
                                    line_total
                                )
                            VALUES
                                (
                                    @p0,
                                    @p1,
                                    @p2,
                                    @p3,
                                    @p4,
                                    @p5
                                );
                            ",
                            orderId,
                            item.BookId,
                            item.Quantity,
                            item.UnitPrice,
                            0,
                            lineTotal
                        );
                    }

                    // B7: Lưu thông tin thanh toán
                    db.Database.ExecuteSqlCommand(
                        @"
                        INSERT INTO dbo.payments
                            (
                                order_id,
                                payment_method,
                                transaction_code,
                                amount,
                                payment_status,
                                payment_date,
                                created_at
                            )
                        VALUES
                            (
                                @p0,
                                @p1,
                                NULL,
                                @p2,
                                @p3,
                                NULL,
                                SYSDATETIME()
                            );
                        ",
                        orderId,
                        paymentMethod,
                        total,
                        "Cho thanh toan"
                    );

                    // B8: Hoàn tất giao dịch
                    transaction.Commit();

                    Session.Remove(CartKey);

                    ViewBag.OrderCode = orderCode;
                    ViewBag.Total = total;
                    ViewBag.PaymentMethod = paymentMethod;
                    ViewBag.PaymentStatus = "Cho thanh toan";

                    return View("Success");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    System.Diagnostics.Debug.WriteLine(
                        "Lỗi đặt hàng: " + ex.ToString());

                    ModelState.AddModelError(
                        "",
                        "Không thể đặt hàng. Vui lòng kiểm tra lại " +
                        "tồn kho và thông tin đặt hàng.");

                    int? tempPromotionId;
                    string tempError;

                    CalculateCheckout(
                        model,
                        cart,
                        out tempPromotionId,
                        out tempError);

                    return View(model);
                }
            }
        }

        // ==========================================
        // GIẢI PHÓNG DATABASE
        // ==========================================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
