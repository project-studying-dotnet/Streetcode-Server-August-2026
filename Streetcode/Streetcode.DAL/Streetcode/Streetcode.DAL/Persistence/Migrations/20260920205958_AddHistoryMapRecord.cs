using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Streetcode.Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryMapRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_history_map_records_StreetcodeId",
                schema: "streetcode",
                table: "history_map_records");

            migrationBuilder.CreateIndex(
                name: "IX_history_map_records_StreetcodeId_PhysicalStreetcodeNumber",
                schema: "streetcode",
                table: "history_map_records",
                columns: new[] { "StreetcodeId", "PhysicalStreetcodeNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_history_map_records_StreetcodeId_PhysicalStreetcodeNumber",
                schema: "streetcode",
                table: "history_map_records");

            migrationBuilder.CreateIndex(
                name: "IX_history_map_records_StreetcodeId",
                schema: "streetcode",
                table: "history_map_records",
                column: "StreetcodeId");
        }
    }
}
