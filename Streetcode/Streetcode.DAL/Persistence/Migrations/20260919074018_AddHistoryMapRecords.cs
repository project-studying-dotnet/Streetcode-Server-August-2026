using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryMapRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "history_map_records",
                schema: "streetcode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StreetcodeId = table.Column<int>(type: "int", nullable: false),
                    ToponymId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PhysicalStreetcodeNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_history_map_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_history_map_records_streetcodes_StreetcodeId",
                        column: x => x.StreetcodeId,
                        principalSchema: "streetcode",
                        principalTable: "streetcodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_history_map_records_toponyms_ToponymId",
                        column: x => x.ToponymId,
                        principalSchema: "toponyms",
                        principalTable: "toponyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_history_map_records_StreetcodeId",
                schema: "streetcode",
                table: "history_map_records",
                column: "StreetcodeId");

            migrationBuilder.CreateIndex(
                name: "IX_history_map_records_ToponymId",
                schema: "streetcode",
                table: "history_map_records",
                column: "ToponymId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "history_map_records",
                schema: "streetcode");
        }
    }
}
