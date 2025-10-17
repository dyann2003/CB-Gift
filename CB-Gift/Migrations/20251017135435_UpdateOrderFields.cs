using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CB_Gift.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_DesignerSeller_AspNetUsers_DesignerUserId",
                table: "DesignerSeller",
                column: "DesignerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignerSeller_AspNetUsers_SellerUserId",
                table: "DesignerSeller",
                column: "SellerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_AspNetUsers_SellerUserId",
                table: "Order",
                column: "SellerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignerSeller_AspNetUsers_DesignerUserId",
                table: "DesignerSeller");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignerSeller_AspNetUsers_SellerUserId",
                table: "DesignerSeller");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_AspNetUsers_SellerUserId",
                table: "Order");
        }
    }
}
