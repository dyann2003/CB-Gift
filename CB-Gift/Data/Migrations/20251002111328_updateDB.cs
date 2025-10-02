using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CB_Gift.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "DesignerSeller",
                columns: table => new
                {
                    DesignerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SellerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignerSeller", x => new { x.DesignerUserId, x.SellerUserId });
                });

            migrationBuilder.CreateTable(
                name: "EndCustomer",
                columns: table => new
                {
                    CustID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Zipcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ShipState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShipCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShipCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndCustomer", x => x.CustID);
                });

            migrationBuilder.CreateTable(
                name: "OrderStatus",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameVi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatus", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreateByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApproveByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApproveDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    StartDatePlan = table.Column<DateTime>(type: "datetime", nullable: true),
                    StopDatePlan = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlanID);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TagCode = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagsID);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    ItemLink = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Describe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Template = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.ProductID);
                    table.ForeignKey(
                        name: "FK_Product_Category",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "CategoryID");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    EndCustomerID = table.Column<int>(type: "int", nullable: false),
                    SellerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CostScan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActiveTTS = table.Column<bool>(type: "bit", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProductionStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tracking = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    StatusOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Order_EndCustomer",
                        column: x => x.EndCustomerID,
                        principalTable: "EndCustomer",
                        principalColumn: "CustID");
                    table.ForeignKey(
                        name: "FK_Order_OrderStatus",
                        column: x => x.StatusOrder,
                        principalTable: "OrderStatus",
                        principalColumn: "StatusId");
                });

            migrationBuilder.CreateTable(
                name: "ProductVariant",
                columns: table => new
                {
                    ProductVariantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    LengthCM = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    HeightCM = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    WidthCM = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    WeightGram = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ShipCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThicknessMM = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SizeInch = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Layer = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CustomShape = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtraShipping = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariant", x => x.ProductVariantID);
                    table.ForeignKey(
                        name: "FK_Variant_Product",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID");
                });

            migrationBuilder.CreateTable(
                name: "Tags_Product",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    TagsID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags_Product", x => new { x.ProductID, x.TagsID });
                    table.ForeignKey(
                        name: "FK_Tags_Product_Product",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tags_Product_Tags",
                        column: x => x.TagsID,
                        principalTable: "Tags",
                        principalColumn: "TagsID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetail",
                columns: table => new
                {
                    OrderDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    ProductVariantID = table.Column<int>(type: "int", nullable: false),
                    LinkImg = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LinkThanksCard = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LinkFileDesign = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Accessory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    NeedDesign = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedDesignerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetail", x => x.OrderDetailID);
                    table.ForeignKey(
                        name: "FK_OrderDetail_Order",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID");
                    table.ForeignKey(
                        name: "FK_OrderDetail_Variant",
                        column: x => x.ProductVariantID,
                        principalTable: "ProductVariant",
                        principalColumn: "ProductVariantID");
                });

            migrationBuilder.CreateTable(
                name: "OrderDetailDesign",
                columns: table => new
                {
                    DesignID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDetailID = table.Column<int>(type: "int", nullable: false),
                    DesignerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__OrderDet__32B8E17F610ED9F4", x => x.DesignID);
                    table.ForeignKey(
                        name: "FK_ODDesign_OrderDetail",
                        column: x => x.OrderDetailID,
                        principalTable: "OrderDetail",
                        principalColumn: "OrderDetailID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanDetail",
                columns: table => new
                {
                    PlanDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanID = table.Column<int>(type: "int", nullable: false),
                    OrderDetailID = table.Column<int>(type: "int", nullable: false),
                    StatusOrder = table.Column<int>(type: "int", nullable: true),
                    NumberOfFinishedProducts = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanDetail", x => x.PlanDetailID);
                    table.ForeignKey(
                        name: "FK_PlanDetail_OrderDetail",
                        column: x => x.OrderDetailID,
                        principalTable: "OrderDetail",
                        principalColumn: "OrderDetailID");
                    table.ForeignKey(
                        name: "FK_PlanDetail_Plan",
                        column: x => x.PlanID,
                        principalTable: "Plans",
                        principalColumn: "PlanID");
                });

            migrationBuilder.CreateTable(
                name: "QC",
                columns: table => new
                {
                    QCCheckID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanDetailID = table.Column<int>(type: "int", nullable: false),
                    CheckedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CheckedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    QuantityChecked = table.Column<int>(type: "int", nullable: true),
                    QuantityPassed = table.Column<int>(type: "int", nullable: true),
                    QuantityFailed = table.Column<int>(type: "int", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QC", x => x.QCCheckID);
                    table.ForeignKey(
                        name: "FK_QC_PlanDetail",
                        column: x => x.PlanDetailID,
                        principalTable: "PlanDetail",
                        principalColumn: "PlanDetailID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DesignerSeller_Seller",
                table: "DesignerSeller",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_EndCustomerID",
                table: "Order",
                column: "EndCustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Order_SellerUserId",
                table: "Order",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_StatusOrder",
                table: "Order",
                column: "StatusOrder");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_AssignedDesignerUserId",
                table: "OrderDetail",
                column: "AssignedDesignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_NeedDesign",
                table: "OrderDetail",
                column: "NeedDesign");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_OrderID",
                table: "OrderDetail",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_ProductVariantID",
                table: "OrderDetail",
                column: "ProductVariantID");

            migrationBuilder.CreateIndex(
                name: "IX_ODDesign_Designer",
                table: "OrderDetailDesign",
                columns: new[] { "DesignerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ODDesign_OrderDetailID",
                table: "OrderDetailDesign",
                column: "OrderDetailID");

            migrationBuilder.CreateIndex(
                name: "UX_ODDesign_Final",
                table: "OrderDetailDesign",
                column: "OrderDetailID",
                unique: true,
                filter: "([IsFinal]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_PlanDetail_OrderDetailID",
                table: "PlanDetail",
                column: "OrderDetailID");

            migrationBuilder.CreateIndex(
                name: "IX_PlanDetail_PlanID",
                table: "PlanDetail",
                column: "PlanID");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_ApproveByUserId",
                table: "Plans",
                column: "ApproveByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_CreateByUserId",
                table: "Plans",
                column: "CreateByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryID",
                table: "Product",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductID",
                table: "ProductVariant",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_QC_CheckedByUserId",
                table: "QC",
                column: "CheckedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QC_PlanDetailID",
                table: "QC",
                column: "PlanDetailID");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Product_TagsID",
                table: "Tags_Product",
                column: "TagsID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesignerSeller");

            migrationBuilder.DropTable(
                name: "OrderDetailDesign");

            migrationBuilder.DropTable(
                name: "QC");

            migrationBuilder.DropTable(
                name: "Tags_Product");

            migrationBuilder.DropTable(
                name: "PlanDetail");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "OrderDetail");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "ProductVariant");

            migrationBuilder.DropTable(
                name: "EndCustomer");

            migrationBuilder.DropTable(
                name: "OrderStatus");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Category");
        }
    }
}
