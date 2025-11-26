namespace CB_Gift.Orders.Import
{
    /// <summary>
    /// Kết quả trả về sau khi import Excel.
    /// Dùng để hiển thị tổng số dòng, số dòng thành công, và danh sách lỗi theo từng dòng.
    /// </summary>
    public class OrderImportResult
    {
        /// <summary>
        /// Tổng số dòng (không tính header).
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// Số dòng tạo Order thành công.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Danh sách lỗi theo dòng.
        /// </summary>
        public List<OrderImportRowError> Errors { get; set; } = new();
    }

    /// <summary>
    /// Lỗi chi tiết của từng dòng trong Excel.
    /// </summary>
    public class OrderImportRowError
    {
        /// <summary>
        /// Số dòng trong Excel (RowNumber).
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Danh sách message lỗi cho dòng này.
        /// </summary>
        public List<string> Messages { get; set; } = new();
    }
}
