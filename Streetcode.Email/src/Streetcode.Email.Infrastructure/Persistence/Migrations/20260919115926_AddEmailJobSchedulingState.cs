using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailJobSchedulingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsJobScheduled",
                table: "EmailDeliveries",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsJobScheduled",
                table: "EmailDeliveries");
        }
    }
}
