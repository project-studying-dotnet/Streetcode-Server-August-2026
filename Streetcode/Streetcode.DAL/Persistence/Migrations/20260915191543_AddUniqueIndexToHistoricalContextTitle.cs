using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Streetcode.DAL.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToHistoricalContextTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM [timeline].[historical_contexts]
                    GROUP BY [Title]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000,
                        'Cannot create unique index because historical context titles contain duplicates.',
                        1;
                END
                """);
            migrationBuilder.CreateIndex(
                name: "IX_historical_contexts_Title",
                schema: "timeline",
                table: "historical_contexts",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_historical_contexts_Title",
                schema: "timeline",
                table: "historical_contexts");
        }
    }
}
