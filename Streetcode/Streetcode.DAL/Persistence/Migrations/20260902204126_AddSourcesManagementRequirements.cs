using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcesManagementRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [sources].[source_link_categories]
                    WHERE DATALENGTH([Title]) / 2 > 23)
                BEGIN
                    THROW 51000,
                        'Cannot limit source category titles to 23 characters because longer values exist.',
                        1;
                END;

                IF EXISTS (
                    SELECT [Title]
                    FROM [sources].[source_link_categories]
                    GROUP BY [Title]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 51000,
                        'Cannot create unique source category title index because duplicate titles exist.',
                        1;
                END;

                IF EXISTS (
                    SELECT [ImageId]
                    FROM [sources].[source_link_categories]
                    GROUP BY [ImageId]
                    HAVING COUNT(*) > 1)
                BEGIN
                    THROW 51000,
                        'Cannot create unique source category image index because duplicate image references exist.',
                        1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_source_link_categories_ImageId",
                schema: "sources",
                table: "source_link_categories");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "sources",
                table: "streetcode_source_link_categories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "sources",
                table: "source_link_categories",
                type: "nvarchar(23)",
                maxLength: 23,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ImageHash",
                schema: "sources",
                table: "source_link_categories",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_link_categories_ImageHash",
                schema: "sources",
                table: "source_link_categories",
                column: "ImageHash",
                unique: true,
                filter: "[ImageHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_source_link_categories_ImageId",
                schema: "sources",
                table: "source_link_categories",
                column: "ImageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_link_categories_Title",
                schema: "sources",
                table: "source_link_categories",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [sources].[streetcode_source_link_categories]
                    WHERE DATALENGTH([Text]) / 2 > 1000)
                BEGIN
                    THROW 51000,
                        'Cannot roll back source text limit because values longer than 1000 characters exist.',
                        1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_source_link_categories_ImageHash",
                schema: "sources",
                table: "source_link_categories");

            migrationBuilder.DropIndex(
                name: "IX_source_link_categories_ImageId",
                schema: "sources",
                table: "source_link_categories");

            migrationBuilder.DropIndex(
                name: "IX_source_link_categories_Title",
                schema: "sources",
                table: "source_link_categories");

            migrationBuilder.DropColumn(
                name: "ImageHash",
                schema: "sources",
                table: "source_link_categories");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "sources",
                table: "streetcode_source_link_categories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "sources",
                table: "source_link_categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(23)",
                oldMaxLength: 23);

            migrationBuilder.CreateIndex(
                name: "IX_source_link_categories_ImageId",
                schema: "sources",
                table: "source_link_categories",
                column: "ImageId");
        }
    }
}
