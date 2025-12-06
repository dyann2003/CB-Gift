namespace CB_Gift.Orders.Import
{
    /// <summary>
    /// Kết quả trả về sau khi import Excel.
    /// Dùng để hiển thị tổng số dòng, số dòng thành công, và danh sách lỗi theo từng dòng.
    /// </summary>
    public class OrderImportResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public List<OrderImportRowError> Errors { get; set; } = new();


        /// <summary>
        /// If created, error report workbook bytes (xlsx) — optional.
        /// </summary>
        public byte[]? ErrorReportFileBytes { get; set; }


        /// <summary>
        /// Suggested filename for error report when returning as file.
        /// </summary>
        public string? ErrorReportFileName { get; set; }
    }

    public class OrderImportRowError
    {
        public int RowNumber { get; set; }
        public List<string> Messages { get; set; } = new();
    }
}
