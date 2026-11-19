
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartBook.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(150)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(
            @"^[0-9]{9,11}$",
            ErrorMessage = "Số điện thoại phải có từ 9 đến 11 chữ số."
        )]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
        [StringLength(255)]
        public string Address { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn vị vận chuyển.")]
        public string ShippingPartner { get; set; }

        public string VoucherCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; }

        public List<CartItemViewModel> Items { get; set; }

        public decimal Subtotal { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal ShippingDiscount { get; set; }

        public decimal VoucherDiscount { get; set; }

        public decimal GrandTotal { get; set; }
    }
}
