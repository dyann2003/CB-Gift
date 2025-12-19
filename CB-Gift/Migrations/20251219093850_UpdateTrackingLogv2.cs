using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CB_Gift.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTrackingLogv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResonCancel",
                table: "GhnTrackingLogs",
                newName: "ReasonCancel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReasonCancel",
                table: "GhnTrackingLogs",
                newName: "ResonCancel");
        }
    }
}
