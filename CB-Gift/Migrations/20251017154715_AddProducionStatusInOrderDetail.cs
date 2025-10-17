using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CB_Gift.Migrations
{
    /// <inheritdoc />
    public partial class AddProducionStatusInOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // THAY THẾ lệnh AddColumn ban đầu bằng AlterColumn.
            migrationBuilder.AlterColumn<string>(
                name: "ProductionStatus",
                table: "OrderDetail",
                // Đảm bảo kiểu dữ liệu là VARCHAR(200) và dùng Converter
                type: "varchar(200)",
                nullable: true) // Giữ nullable: true nếu bạn không muốn gán giá trị mặc định cho các bản ghi cũ
                .Annotation("MySql:CharSet", "utf8mb4");

            // LƯU Ý: Nếu cột ProductionStatus ban đầu được tạo với nullable: false, 
            // bạn cần phải đảm bảo AlterColumn có nullable: false hoặc đã có giá trị mặc định.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductionStatus",
                table: "OrderDetail");
        }
    }
}
