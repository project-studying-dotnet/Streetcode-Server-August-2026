using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderToFact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "streetcode",
                table: "facts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                EXEC sp_executesql N'
                    UPDATE f
                    SET f.DisplayOrder = ranked.RowNum
                    FROM [streetcode].[facts] f
                    INNER JOIN (
                        SELECT Id,
                               ROW_NUMBER() OVER (
                                   PARTITION BY StreetcodeId
                                   ORDER BY Id
                               ) AS RowNum
                        FROM [streetcode].[facts]
                    ) AS ranked ON f.Id = ranked.Id;
                ';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "streetcode",
                table: "facts");
        }
    }
}
