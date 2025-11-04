using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CB_Gift.Migrations
{
    /// <inheritdoc />
    public partial class ModifyRefundsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Refunds_AspNetUsers_RefundedByStaffId",
                table: "Refunds");

            migrationBuilder.RenameColumn(
                name: "RefundedByStaffId",
                table: "Refunds",
                newName: "ReviewedByStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_Refunds_RefundedByStaffId",
                table: "Refunds",
                newName: "IX_Refunds_ReviewedByStaffId");

            migrationBuilder.UpdateData(
                table: "Refunds",
                keyColumn: "Reason",
                keyValue: null,
                column: "Reason",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Refunds",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Refunds",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofUrl",
                table: "Refunds",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RequestedBySellerId",
                table: "Refunds",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Refunds",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffRejectionReason",
                table: "Refunds",
                type: "TEXT",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Refunds",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RequestedBySellerId",
                table: "Refunds",
                column: "RequestedBySellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Refunds_AspNetUsers_RequestedBySellerId",
                table: "Refunds",
                column: "RequestedBySellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Refunds_AspNetUsers_ReviewedByStaffId",
                table: "Refunds",
                column: "ReviewedByStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Refunds_AspNetUsers_RequestedBySellerId",
                table: "Refunds");

            migrationBuilder.DropForeignKey(
                name: "FK_Refunds_AspNetUsers_ReviewedByStaffId",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_Refunds_RequestedBySellerId",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "ProofUrl",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "RequestedBySellerId",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "StaffRejectionReason",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Refunds");

            migrationBuilder.RenameColumn(
                name: "ReviewedByStaffId",
                table: "Refunds",
                newName: "RefundedByStaffId");

            migrationBuilder.RenameIndex(
                name: "IX_Refunds_ReviewedByStaffId",
                table: "Refunds",
                newName: "IX_Refunds_RefundedByStaffId");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Refunds",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Refunds",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_Refunds_AspNetUsers_RefundedByStaffId",
                table: "Refunds",
                column: "RefundedByStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
