
using System;

namespace SmartBook.Models
{
    public class AdminOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
    }
}
