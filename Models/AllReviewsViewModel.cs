
using System;
using System.Collections.Generic;

namespace SmartBook.Models
{
    public class AllReviewsViewModel
    {
        public int TotalReviews { get; set; }

        public decimal AverageRating { get; set; }

        public List<AllReviewItemViewModel> Reviews { get; set; }

        public AllReviewsViewModel()
        {
            Reviews = new List<AllReviewItemViewModel>();
        }
    }

    public class AllReviewItemViewModel
    {
        public int ReviewId { get; set; }

        public int BookId { get; set; }

        public string BookTitle { get; set; }

        public string CustomerName { get; set; }

        public int Rating { get; set; }

        public string ReviewTitle { get; set; }

        public string ReviewContent { get; set; }

        public DateTime ReviewDate { get; set; }
    }
}
