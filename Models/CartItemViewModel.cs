
using System;

namespace SmartBook.Models
{
    public class CartItemViewModel
    {
        // ID sách trong database
        public int BookId { get; set; }

        // Tên sách
        public string BookTitle { get; set; }

        // Tên tác giả
        public string AuthorName { get; set; }

        // Đường dẫn ảnh bìa
        public string ImageUrl { get; set; }

        // Đơn giá
        public decimal UnitPrice { get; set; }

        // Số lượng
        public int Quantity { get; set; }

        // Thành tiền
        public decimal LineTotal
        {
            get
            {
                return UnitPrice * Quantity;
            }
        }
    }
}
