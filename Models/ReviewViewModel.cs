
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartBook.Models
{
    public class ReviewViewModel
    {
        public int BookId { get; set; }

        public int CustomerId { get; set; }

        public int? OrderId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao.")]
        public int Rating { get; set; }

        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        public string ReviewTitle { get; set; }

        [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự.")]
        public string ReviewContent { get; set; }
    }

    public class ReviewItemViewModel
    {
        public int ReviewId { get; set; }

        public string CustomerName { get; set; }

        public int Rating { get; set; }

        public string ReviewTitle { get; set; }

        public string ReviewContent { get; set; }

        public DateTime ReviewDate { get; set; }
    }

    public class BookReviewsViewModel
    {
        public int BookId { get; set; }

        public decimal AverageRating { get; set; }

        public int TotalReviews { get; set; }

        public ReviewViewModel NewReview { get; set; }

        public List<ReviewItemViewModel> Reviews { get; set; }
    }
}
