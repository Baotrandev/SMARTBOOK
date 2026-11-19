
using System;

namespace SmartBook.Models
{
    public class OrderHistoryViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string CustomerNote { get; set; }
        public int TotalItems { get; set; }
    }
}
