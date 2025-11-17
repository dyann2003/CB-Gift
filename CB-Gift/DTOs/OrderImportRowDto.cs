namespace CB_Gift.Orders.Import
{
    /// <summary>
    /// DTO đại diện cho 1 dòng trong file Excel import Order.
    /// Dùng để:
    ///  - Đọc dữ liệu từ Excel
    ///  - Validate bằng FluentValidation
    ///  - Map sang EndCustomer + Order + OrderDetail
    /// </summary>
    public class OrderImportRowDto
    {
        /// <summary>
        /// Số dòng trong Excel (để báo lỗi chính xác).
        /// </summary>
        public int RowNumber { get; set; }
        public int Quantity { get; set; }
        // ========= Thông tin đơn hàng =========
        public String OrderID { get; set; }
        public string? OrderCode { get; set; }
        public DateTime? OrderDate { get; set; }

        public string? CustomerName { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string? Address { get; set; }
        public string? Zipcode { get; set; }

        /// <summary>
        /// Tỉnh/Thành (VD: "Hồ Chí Minh", "Hà Nội").
        /// </summary>
        public string? ShipState { get; set; }

        /// <summary>
        /// Quận/Huyện/Thành phố trực thuộc (VD: "Quận 1").
        /// </summary>
        public string? ShipCity { get; set; }

        /// <summary>
        /// Quốc gia – luôn phải là Việt Nam.
        /// </summary>
        public string? ShipCountry { get; set; }

        public string? PaymentStatus { get; set; }
        public string? Note { get; set; }
        /// <summary>
        /// Bật Text-To-Speech hay không.
        /// </summary>
        public bool? ActiveTTS { get; set; }

        public decimal? TotalCost { get; set; }

        /// <summary>
        /// Trạng thái đơn (map với Order.StatusOrder).
        /// </summary>
        public int StatusOrder { get; set; }

        // ========= Thông tin sản phẩm =========

        /// <summary>
        /// Tên Product (VD: "SUN-WOOD-ACR-MIRROR", "SUN-GLASS").
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Size theo inch (VD: "8IN", "10IN").
        /// </summary>
        public string? SizeInch { get; set; }

        public string? Accessory { get; set; }

        public string? LinkImg { get; set; }
        public string? LinkThanksCard { get; set; }
        public string? LinkFileDesign { get; set; }

        /// <summary>
        /// Thành tiền cho dòng sản phẩm (nếu có).
        /// </summary>
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// Ghi chú cho đơn / sản phẩm.
        /// </summary>
        public string? OrderNotes { get; set; }

        /// <summary>
        /// Thời điểm tạo đơn (nếu Excel có cột này).
        /// </summary>
        public DateTime? TimeCreated { get; set; }
    }
}
