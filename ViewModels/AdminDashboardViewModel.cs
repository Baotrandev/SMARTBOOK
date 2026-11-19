
using System;
using System.Collections.Generic;

namespace SmartBook.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPurchases { get; set; }
        public int TotalReviews { get; set; }
        public int ApprovedReviews { get; set; }

        public List<MonthlyRevenueViewModel> MonthlyRevenue { get; set; }
            = new List<MonthlyRevenueViewModel>();

        public List<RecentOrderViewModel> RecentOrders { get; set; }
            = new List<RecentOrderViewModel>();
    }

    public class MonthlyRevenueViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentOrderViewModel
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
