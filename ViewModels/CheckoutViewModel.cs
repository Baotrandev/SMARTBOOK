
using SmartBook.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartBook.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^[0-9]{9,11}$",
            ErrorMessage = "Số điện thoại phải có từ 9 đến 11 chữ số.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
        public string Address { get; set; }

        public string Note { get; set; }

        [Required]
        public string ShippingPartner { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public string VoucherCode { get; set; }

        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal ShippingDiscount { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal GrandTotal { get; set; }

        public List<CartItemViewModel> Items { get; set; }
    }
}
