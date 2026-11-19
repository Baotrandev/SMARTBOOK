
using System;

namespace SmartBook.Models
{
    public class AdminReviewViewModel
    {
        public int ReviewId { get; set; }

        public int CustomerId { get; set; }

        public int BookId { get; set; }

        public int? OrderId { get; set; }

        public string CustomerName { get; set; }

        public string BookTitle { get; set; }

        public int Rating { get; set; }

        public string ReviewTitle { get; set; }

        public string ReviewContent { get; set; }

        public DateTime ReviewDate { get; set; }

        public bool IsApproved { get; set; }
    }
}
