namespace CB_Gift.Models.Enums
{
    public enum ProductionStatus
    {
        DRAFT, //0
        // Trạng thái ban đầu
        CREATED, //1

        // Trạng thái liên quan đến Thiết kế
        NEED_DESIGN, //2
        DESIGNING, //3
        CHECK_DESIGN, //4
        DESIGN_REDO,  //5

        // Sẵn sàng sản xuất
        READY_PROD, // Design được chấp nhận.  //6

        // Trạng thái liên quan đến Sản xuất  
        IN_PROD, //7
        FINISHED, //8
        QC_DONE, //9    
        QC_FAIL, //10
        PROD_REWORK, //11

        // Đóng gói/Giao hàng
        PACKING, //12

        // Khác
        HOLD,  //13
        CANCELLED //14
    }
}
