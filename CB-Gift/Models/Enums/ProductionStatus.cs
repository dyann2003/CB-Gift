namespace CB_Gift.Models.Enums
{
    public enum ProductionStatus
    {
        DRAFT,
        // Trạng thái ban đầu
        CREATED,

        // Trạng thái liên quan đến Thiết kế
        NEED_DESIGN,
        DESIGNING,
        CHECK_DESIGN,
        DESIGN_REDO,

        // Sẵn sàng sản xuất
        READY_PROD, // Design được chấp nhận.

        // Trạng thái liên quan đến Sản xuất
        IN_PROD,
        FINISHED,
        QC_DONE,
        QC_FAIL,
        PROD_REWORK,

        // Đóng gói/Giao hàng
        PACKING,

        // Khác
        HOLD,
        CANCELLED
    }
}
