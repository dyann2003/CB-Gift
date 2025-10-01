IF OBJECT_ID(N'dbo.PlanDetail_OrderDetail', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlanDetail_OrderDetail (
        PlanDetailOrderDetailID INT IDENTITY(1,1) PRIMARY KEY,     -- ID tự tăng
        PlanDetailID            INT NOT NULL,                      -- FK → PlanDetail
        OrderDetailID           INT NOT NULL,                      -- FK → OrderDetail
        CreatedDate             DATETIME NOT NULL DEFAULT GETDATE(),
        Note                    NVARCHAR(255) NULL,

        CONSTRAINT FK_PDOD_PlanDetail FOREIGN KEY (PlanDetailID)
            REFERENCES dbo.PlanDetail(PlanDetailID) ON DELETE CASCADE,

        CONSTRAINT FK_PDOD_OrderDetail FOREIGN KEY (OrderDetailID)
            REFERENCES dbo.OrderDetail(OrderDetailID) ON DELETE CASCADE,

        CONSTRAINT UQ_PDOD_Unique UNIQUE (PlanDetailID, OrderDetailID)  -- Không trùng đơn trong 1 batch
    );

    CREATE NONCLUSTERED INDEX IX_PDOD_PlanDetailID ON dbo.PlanDetail_OrderDetail(PlanDetailID);
    CREATE NONCLUSTERED INDEX IX_PDOD_OrderDetailID ON dbo.PlanDetail_OrderDetail(OrderDetailID);
END;
GO



