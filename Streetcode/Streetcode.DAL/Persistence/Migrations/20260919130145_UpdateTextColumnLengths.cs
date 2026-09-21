using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTextColumnLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [streetcode].[texts]
                    WHERE DATALENGTH([Title]) / 2 > 50
                )
                BEGIN
                    THROW 50001, 'Cannot limit texts.Title to 50 characters because longer values exist.', 1;
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "streetcode",
                table: "texts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [streetcode].[texts]
                    WHERE DATALENGTH([AdditionalText]) / 2 > 200
                )
                BEGIN
                    THROW 50001, 'Cannot limit texts.AdditionalText to 200 characters because longer values exist.', 1;
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalText",
                schema: "streetcode",
                table: "texts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
                    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "streetcode",
                table: "texts",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "AdditionalText",
                schema: "streetcode",
                table: "texts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
