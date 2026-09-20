using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEmailSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailDeliveries",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Template = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveries", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveries_CorrelationId",
                table: "EmailDeliveries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveries_Status",
                table: "EmailDeliveries",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDeliveries");
        }
    }
}
