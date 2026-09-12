using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageAssigmentAndShortDescriptionToStreetcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                schema: "streetcode",
                table: "streetcodes",
                type: "nvarchar(33)",
                maxLength: 33,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageAssigment",
                schema: "streetcode",
                table: "streetcode_image",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortDescription",
                schema: "streetcode",
                table: "streetcodes");

            migrationBuilder.DropColumn(
                name: "ImageAssigment",
                schema: "streetcode",
                table: "streetcode_image");
        }
    }
}
