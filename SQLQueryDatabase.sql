/* =======================
   CREATE DATABASE
   ======================= */
IF DB_ID(N'CBGift') IS NULL
BEGIN
    CREATE DATABASE [CBGift];
END
GO
USE [CBGift];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =======================
   ASP.NET IDENTITY TABLES đã migration ra rồi, không cần tạo nữa.
   ======================= */


/* =======================
   LOOKUPS
   ======================= */
IF OBJECT_ID(N'dbo.OrderStatus','U') IS NULL
CREATE TABLE [dbo].[OrderStatus](
    [StatusId]  INT NOT NULL CONSTRAINT [PK_OrderStatus] PRIMARY KEY,
    [Code]      NVARCHAR(50) NOT NULL,
    [NameVi]    NVARCHAR(100) NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.OrderStatus)
INSERT dbo.OrderStatus([StatusId],[Code],[NameVi]) VALUES
(1,N'CREATED',N'Lên đơn (24h)'),
(2,N'CONFIRMED',N'Chốt đơn (khóa seller)'),
(3,N'BUNDLED',N'Gom đơn'),
(4,N'INPROD',N'Đang sản xuất'),
(5,N'FINISHED',N'Sản xuất xong'),
(6,N'SHIPPED',N'Đã ship'),
(7,N'REWORK',N'Sản xuất lại'),
(8,N'CANCELLED',N'Cancel'),
(9,N'NEEDDESIGN',N'Cần Design'),
(10,N'REFUND',N'Hoàn hàng');
GO

/* =======================
   BUSINESS TABLES
   ======================= */

-- Category (DanhMuc)
IF OBJECT_ID(N'dbo.Category','U') IS NULL
CREATE TABLE [dbo].[Category](
    [CategoryID]   INT IDENTITY(1,1) NOT NULL,
    [CategoryName] NVARCHAR(100) NOT NULL,
    [CategoryCode] NVARCHAR(50)  NULL,
    [Status]       INT NULL,
    CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED([CategoryID] ASC)
);
GO

-- Tags
IF OBJECT_ID(N'dbo.Tags','U') IS NULL
CREATE TABLE [dbo].[Tags](
    [TagsID]  INT IDENTITY(1,1) NOT NULL,
    [TagName] NVARCHAR(100) NULL,
    [TagCode] VARCHAR(150) NULL,
    CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED([TagsID] ASC)
);
GO

-- Product (SanPham)
IF OBJECT_ID(N'dbo.Product','U') IS NULL
CREATE TABLE [dbo].[Product](
    [ProductID]   INT IDENTITY(1,1) NOT NULL,
    [CategoryID]  INT NOT NULL,
    [ProductName] NVARCHAR(100) NULL,
    [ProductCode] NVARCHAR(50) NULL,
    [Status]      INT NULL,
    [ItemLink]    NVARCHAR(200) NULL,
    [Describe]    NVARCHAR(500) NULL,
    [Template]    NVARCHAR(500) NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED([ProductID] ASC)
);
GO

-- Tags_Product (bảng nối Tag–Product)
IF OBJECT_ID(N'dbo.Tags_Product','U') IS NULL
CREATE TABLE [dbo].[Tags_Product](
    [ProductID] INT NOT NULL,
    [TagsID]    INT NOT NULL,
    CONSTRAINT [PK_Tags_Product] PRIMARY KEY CLUSTERED([ProductID],[TagsID])
);
GO

-- ProductVariant (Biến thể)
IF OBJECT_ID(N'dbo.ProductVariant','U') IS NULL
CREATE TABLE [dbo].[ProductVariant](
    [ProductVariantID] INT IDENTITY(1,1) NOT NULL,
    [ProductID]        INT NOT NULL,
    [LengthCM]         DECIMAL(10,2) NULL,
    [HeightCM]         DECIMAL(10,2) NULL,
    [WidthCM]          DECIMAL(10,2) NULL,
    [WeightGram]       DECIMAL(10,2) NULL,
    [ShipCost]         DECIMAL(18,2) NULL,
    [BaseCost]         DECIMAL(18,2) NULL,
    [ThicknessMM]      VARCHAR(50)  NOT NULL,
    [SizeInch]         VARCHAR(100) NOT NULL,
    [Layer]            VARCHAR(100) NOT NULL,
    [CustomShape]      NVARCHAR(200) NOT NULL,
    [SKU]              NVARCHAR(500) NULL,
    [ExtraShipping]    DECIMAL(18,2) NULL,
    [TotalCost]        DECIMAL(18,2) NULL,
    CONSTRAINT [PK_ProductVariant] PRIMARY KEY CLUSTERED([ProductVariantID] ASC)
);
GO

-- EndCustomer
IF OBJECT_ID(N'dbo.EndCustomer','U') IS NULL
CREATE TABLE [dbo].[EndCustomer](
    [CustID]      INT IDENTITY(1,1) NOT NULL,
    [Name]        NVARCHAR(100) NOT NULL,
    [Phone]       NVARCHAR(20) NULL,
    [Email]       NVARCHAR(100) NULL,
    [Address]     NVARCHAR(200) NULL,
    [Address1]    NVARCHAR(200) NULL,
    [Zipcode]     NVARCHAR(20) NULL,
    [ShipState]   NVARCHAR(50) NULL,
    [ShipCity]    NVARCHAR(50) NULL,
    [ShipCountry] NVARCHAR(50) NULL,
    CONSTRAINT [PK_EndCustomer] PRIMARY KEY CLUSTERED([CustID] ASC)
);
GO

-- Order (A.Order) — dùng AspNetUsers thay Seller
IF OBJECT_ID(N'dbo.[Order]','U') IS NULL
CREATE TABLE [dbo].[Order](
    [OrderID]         INT IDENTITY(1,1) NOT NULL,
    [OrderCode]       NVARCHAR(100) NULL,
    [OrderDate]       DATETIME NOT NULL CONSTRAINT [DF_Order_OrderDate] DEFAULT(GETDATE()),
    [EndCustomerID]   INT NOT NULL,
    [SellerUserId]    NVARCHAR(450) NOT NULL,   -- thay SellerID
    [CreationDate]    DATETIME NULL CONSTRAINT [DF_Order_CreationDate] DEFAULT(GETDATE()),
	[CostScan]         DECIMAL(18,2) NULL,
	[ActiveTTS]        BIT NULL,
    [TotalCost]       DECIMAL(18,2) NULL,
    [ProductionStatus] NVARCHAR(50) NULL,
    [PaymentStatus]   NVARCHAR(50) NULL,
    [Tracking]        VARCHAR(200) NULL,
    [StatusOrder]     INT NOT NULL CONSTRAINT [DF_Order_StatusOrder] DEFAULT(1), -- FK -> OrderStatus
    CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED([OrderID] ASC)
);
GO

-- OrderDetail
IF OBJECT_ID(N'dbo.OrderDetail','U') IS NULL
CREATE TABLE [dbo].[OrderDetail](
    [OrderDetailID]    INT IDENTITY(1,1) NOT NULL,
    [OrderID]          INT NOT NULL,
    [ProductVariantID] INT NOT NULL,
    [LinkImg]          NVARCHAR(200) NULL,
    [LinkThanksCard]   NVARCHAR(200) NULL,
    [LinkFileDesign]   NVARCHAR(200) NULL,
    [Accessory]        NVARCHAR(200) NULL,
    [Note]             NVARCHAR(200) NULL,
    [CreatedDate]      DATETIME NULL CONSTRAINT [DF_OrderDetail_CreatedDate] DEFAULT(GETDATE()),
    [Quantity]         INT NOT NULL,
    CONSTRAINT [PK_OrderDetail] PRIMARY KEY CLUSTERED([OrderDetailID] ASC)
);
GO

-- Plans (Plan)
IF OBJECT_ID(N'dbo.Plans','U') IS NULL
CREATE TABLE [dbo].[Plans](
    [PlanID]          INT IDENTITY(1,1) NOT NULL,
    [CreateDate]      DATETIME NULL,
    [CreateByUserId]  NVARCHAR(450) NULL,
    [ApproveByUserId] NVARCHAR(450) NULL,
    [ApproveDate]     DATETIME NULL,
    [StartDatePlan]   DATETIME NULL,
    [StopDatePlan]    DATETIME NULL,
    CONSTRAINT [PK_Plans] PRIMARY KEY CLUSTERED([PlanID] ASC)
);
GO

-- PlanDetail
IF OBJECT_ID(N'dbo.PlanDetail','U') IS NULL
CREATE TABLE [dbo].[PlanDetail](
    [PlanDetailID]              INT IDENTITY(1,1) NOT NULL,
    [PlanID]                    INT NOT NULL,
    [OrderDetailID]             INT NOT NULL,
    [StatusOrder]               INT NULL, -- tuỳ muốn dùng lại OrderStatus hay status riêng
    [NumberOfFinishedProducts]  INT NULL,
    CONSTRAINT [PK_PlanDetail] PRIMARY KEY CLUSTERED([PlanDetailID] ASC)
);
GO

-- QC
IF OBJECT_ID(N'dbo.QC','U') IS NULL
CREATE TABLE [dbo].[QC](
    [QCCheckID]       INT IDENTITY(1,1) NOT NULL,
    [PlanDetailID]    INT NOT NULL,
    [CheckedByUserId] NVARCHAR(450) NULL, -- thay CheckedBy text
    [CheckedDate]     DATETIME NULL CONSTRAINT [DF_QC_CheckedDate] DEFAULT(GETDATE()),
    [QuantityChecked] INT NULL,
    [QuantityPassed]  INT NULL,
    [QuantityFailed]  INT NULL,
    [Remark]          NVARCHAR(400) NULL,
    CONSTRAINT [PK_QC] PRIMARY KEY CLUSTERED([QCCheckID] ASC)
);
GO

/* =======================
   FOREIGN KEYS + INDEXES
   ======================= */

-- Product -> Category
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Product_Category')
ALTER TABLE [dbo].[Product]  WITH CHECK
ADD CONSTRAINT [FK_Product_Category]
FOREIGN KEY([CategoryID]) REFERENCES [dbo].[Category]([CategoryID]);
GO
CREATE NONCLUSTERED INDEX [IX_Product_CategoryID] ON [dbo].[Product]([CategoryID]);
GO

-- Tags_Product -> Product, Tags
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tags_Product_Product')
ALTER TABLE [dbo].[Tags_Product] WITH CHECK
ADD CONSTRAINT [FK_Tags_Product_Product]
FOREIGN KEY([ProductID]) REFERENCES [dbo].[Product]([ProductID]) ON DELETE CASCADE;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tags_Product_Tags')
ALTER TABLE [dbo].[Tags_Product] WITH CHECK
ADD CONSTRAINT [FK_Tags_Product_Tags]
FOREIGN KEY([TagsID]) REFERENCES [dbo].[Tags]([TagsID]) ON DELETE CASCADE;
GO

-- ProductVariant -> Product
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Variant_Product')
ALTER TABLE [dbo].[ProductVariant]  WITH CHECK
ADD CONSTRAINT [FK_Variant_Product]
FOREIGN KEY([ProductID]) REFERENCES [dbo].[Product]([ProductID]);
GO
CREATE NONCLUSTERED INDEX [IX_ProductVariant_ProductID] ON [dbo].[ProductVariant]([ProductID]);
GO

-- Order -> EndCustomer
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Order_EndCustomer')
ALTER TABLE [dbo].[Order] WITH CHECK
ADD CONSTRAINT [FK_Order_EndCustomer]
FOREIGN KEY([EndCustomerID]) REFERENCES [dbo].[EndCustomer]([CustID]);
GO
CREATE NONCLUSTERED INDEX [IX_Order_EndCustomerID] ON [dbo].[Order]([EndCustomerID]);
GO

-- Order -> AspNetUsers (SellerUserId)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Order_SellerUser')
ALTER TABLE [dbo].[Order] WITH CHECK
ADD CONSTRAINT [FK_Order_SellerUser]
FOREIGN KEY([SellerUserId]) REFERENCES [dbo].[AspNetUsers]([Id]);
GO
CREATE NONCLUSTERED INDEX [IX_Order_SellerUserId] ON [dbo].[Order]([SellerUserId]);
GO

-- Order -> OrderStatus
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Order_OrderStatus')
ALTER TABLE [dbo].[Order] WITH CHECK
ADD CONSTRAINT [FK_Order_OrderStatus]
FOREIGN KEY([StatusOrder]) REFERENCES [dbo].[OrderStatus]([StatusId]);
GO
CREATE NONCLUSTERED INDEX [IX_Order_StatusOrder] ON [dbo].[Order]([StatusOrder]);
GO

-- OrderDetail -> Order
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderDetail_Order')
ALTER TABLE [dbo].[OrderDetail] WITH CHECK
ADD CONSTRAINT [FK_OrderDetail_Order]
FOREIGN KEY([OrderID]) REFERENCES [dbo].[Order]([OrderID]);
GO
CREATE NONCLUSTERED INDEX [IX_OrderDetail_OrderID] ON [dbo].[OrderDetail]([OrderID]);
GO

-- OrderDetail -> ProductVariant
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderDetail_Variant')
ALTER TABLE [dbo].[OrderDetail] WITH CHECK
ADD CONSTRAINT [FK_OrderDetail_Variant]
FOREIGN KEY([ProductVariantID]) REFERENCES [dbo].[ProductVariant]([ProductVariantID]);
GO
CREATE NONCLUSTERED INDEX [IX_OrderDetail_ProductVariantID] ON [dbo].[OrderDetail]([ProductVariantID]);
GO

-- PlanDetail -> Plans
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanDetail_Plan')
ALTER TABLE [dbo].[PlanDetail] WITH CHECK
ADD CONSTRAINT [FK_PlanDetail_Plan]
FOREIGN KEY([PlanID]) REFERENCES [dbo].[Plans]([PlanID]);
GO
CREATE NONCLUSTERED INDEX [IX_PlanDetail_PlanID] ON [dbo].[PlanDetail]([PlanID]);
GO

-- PlanDetail -> OrderDetail
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlanDetail_OrderDetail')
ALTER TABLE [dbo].[PlanDetail] WITH CHECK
ADD CONSTRAINT [FK_PlanDetail_OrderDetail]
FOREIGN KEY([OrderDetailID]) REFERENCES [dbo].[OrderDetail]([OrderDetailID]);
GO
CREATE NONCLUSTERED INDEX [IX_PlanDetail_OrderDetailID] ON [dbo].[PlanDetail]([OrderDetailID]);
GO

-- Plans.CreateByUserId -> AspNetUsers
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Plans_CreateByUser')
ALTER TABLE [dbo].[Plans] WITH CHECK
ADD CONSTRAINT [FK_Plans_CreateByUser]
FOREIGN KEY([CreateByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]);
GO
CREATE NONCLUSTERED INDEX [IX_Plans_CreateByUserId] ON [dbo].[Plans]([CreateByUserId]);
GO

-- Plans.ApproveByUserId -> AspNetUsers
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Plans_ApproveByUser')
ALTER TABLE [dbo].[Plans] WITH CHECK
ADD CONSTRAINT [FK_Plans_ApproveByUser]
FOREIGN KEY([ApproveByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]);
GO
CREATE NONCLUSTERED INDEX [IX_Plans_ApproveByUserId] ON [dbo].[Plans]([ApproveByUserId]);
GO

-- QC -> PlanDetail
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QC_PlanDetail')
ALTER TABLE [dbo].[QC] WITH CHECK
ADD CONSTRAINT [FK_QC_PlanDetail]
FOREIGN KEY([PlanDetailID]) REFERENCES [dbo].[PlanDetail]([PlanDetailID]);
GO
CREATE NONCLUSTERED INDEX [IX_QC_PlanDetailID] ON [dbo].[QC]([PlanDetailID]);
GO

-- QC.CheckedByUserId -> AspNetUsers
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QC_CheckedByUser')
ALTER TABLE [dbo].[QC] WITH CHECK
ADD CONSTRAINT [FK_QC_CheckedByUser]
FOREIGN KEY([CheckedByUserId]) REFERENCES [dbo].[AspNetUsers]([Id]);
GO
CREATE NONCLUSTERED INDEX [IX_QC_CheckedByUserId] ON [dbo].[QC]([CheckedByUserId]);
GO
--thêm desginer
IF OBJECT_ID(N'dbo.OrderDetailDesign','U') IS NULL
CREATE TABLE dbo.OrderDetailDesign(
    DesignID        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderDetailID   INT NOT NULL,
    DesignerUserId  NVARCHAR(450) NOT NULL,
    FileUrl         NVARCHAR(500) NOT NULL,
    Note            NVARCHAR(400) NULL,
    IsFinal         BIT NOT NULL CONSTRAINT DF_OrderDetailDesign_IsFinal DEFAULT(0),
    CreatedAt       DATETIME NOT NULL CONSTRAINT DF_OrderDetailDesign_CreatedAt DEFAULT(GETDATE())
);
GO
ALTER TABLE dbo.OrderDetailDesign WITH CHECK
ADD CONSTRAINT FK_ODDesign_OrderDetail
    FOREIGN KEY (OrderDetailID) REFERENCES dbo.OrderDetail(OrderDetailID) ON DELETE CASCADE;
ALTER TABLE dbo.OrderDetailDesign WITH CHECK
ADD CONSTRAINT FK_ODDesign_Designer
    FOREIGN KEY (DesignerUserId) REFERENCES dbo.AspNetUsers(Id);

CREATE INDEX IX_ODDesign_OrderDetailID ON dbo.OrderDetailDesign(OrderDetailID);
CREATE INDEX IX_ODDesign_Designer     ON dbo.OrderDetailDesign(DesignerUserId, CreatedAt);
-- Mỗi OrderDetail chỉ có 1 bản Final (tuỳ chọn)
CREATE UNIQUE INDEX UX_ODDesign_Final
ON dbo.OrderDetailDesign(OrderDetailID)
WHERE IsFinal = 1;

--- tạo bảng mapping để assign design
/* =========================================================
   0) Đảm bảo đã có AspNetUsers (chỉ kiểm, không tạo)
   ========================================================= */
IF OBJECT_ID(N'dbo.AspNetUsers','U') IS NULL
BEGIN
    RAISERROR('AspNetUsers not found. Please provision Identity tables first.', 16, 1);
    RETURN;
END
GO

/* =========================================================
   1) Bổ sung cột assign vào OrderDetail (nếu chưa có)
   ========================================================= */
IF COL_LENGTH('dbo.OrderDetail','NeedDesign') IS NULL
    ALTER TABLE dbo.OrderDetail ADD NeedDesign BIT NOT NULL CONSTRAINT DF_OrderDetail_NeedDesign DEFAULT(1);
IF COL_LENGTH('dbo.OrderDetail','AssignedDesignerUserId') IS NULL
    ALTER TABLE dbo.OrderDetail ADD AssignedDesignerUserId NVARCHAR(450) NULL;
IF COL_LENGTH('dbo.OrderDetail','AssignedAt') IS NULL
    ALTER TABLE dbo.OrderDetail ADD AssignedAt DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OrderDetail_AssignedDesigner')
BEGIN
    ALTER TABLE dbo.OrderDetail WITH CHECK
    ADD CONSTRAINT FK_OrderDetail_AssignedDesigner
        FOREIGN KEY (AssignedDesignerUserId) REFERENCES dbo.AspNetUsers(Id);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderDetail_AssignedDesignerUserId' AND object_id = OBJECT_ID('dbo.OrderDetail'))
    CREATE INDEX IX_OrderDetail_AssignedDesignerUserId ON dbo.OrderDetail(AssignedDesignerUserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderDetail_NeedDesign' AND object_id = OBJECT_ID('dbo.OrderDetail'))
    CREATE INDEX IX_OrderDetail_NeedDesign ON dbo.OrderDetail(NeedDesign);
GO

/* =========================================================
   2) Bảng mapping Designer ↔ Seller (đều là AspNetUsers)
   ========================================================= */
IF OBJECT_ID(N'dbo.DesignerSeller','U') IS NULL
BEGIN
	CREATE TABLE dbo.DesignerSeller(
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    DesignerUserId  NVARCHAR(450) NOT NULL,
    SellerUserId    NVARCHAR(450) NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT (GETDATE()),
    CreatedByUserId NVARCHAR(450) NULL,
    CONSTRAINT UQ_DesignerSeller UNIQUE (DesignerUserId, SellerUserId),
    CONSTRAINT FK_DesignerSeller_Designer FOREIGN KEY (DesignerUserId) REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT FK_DesignerSeller_Seller   FOREIGN KEY (SellerUserId)   REFERENCES dbo.AspNetUsers(Id),
    CONSTRAINT CK_DesignerSeller_NotSame  CHECK (DesignerUserId <> SellerUserId)
);

END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DesignerSeller_Seller' AND object_id = OBJECT_ID('dbo.DesignerSeller'))
    CREATE INDEX IX_DesignerSeller_Seller ON dbo.DesignerSeller(SellerUserId);
GO

