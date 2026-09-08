// <copyright file="AddCommentsMigrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Persistence
{
    using Microsoft.EntityFrameworkCore.Migrations;
    using Microsoft.EntityFrameworkCore.Migrations.Operations;
    using Streetcode.DAL.Persistence.Migrations;
    using Xunit;

    public class AddCommentsMigrationTests
    {
        [Fact]
        public void Up_ShouldCreateCommentsTableWithStreetcodeForeignKeyAndIndex()
        {
            var migration = new TestableAddCommentsMigration();
            var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

            migration.ApplyUp(migrationBuilder);

            var createTable = Assert.Single(
                migrationBuilder.Operations.OfType<CreateTableOperation>());
            var streetcodeForeignKey = Assert.Single(createTable.ForeignKeys);
            var streetcodeIndex = Assert.Single(
                migrationBuilder.Operations.OfType<CreateIndexOperation>());

            Assert.Equal("comments", createTable.Name);
            Assert.Equal("streetcode", createTable.Schema);
            Assert.Contains(createTable.Columns, column => column.Name == "AuthorId");
            Assert.Contains(createTable.Columns, column => column.Name == "Text" && !column.IsNullable);
            Assert.Equal("StreetcodeId", streetcodeForeignKey.Columns.Single());
            Assert.Equal("streetcodes", streetcodeForeignKey.PrincipalTable);
            Assert.Equal(ReferentialAction.Cascade, streetcodeForeignKey.OnDelete);
            Assert.Equal("IX_comments_StreetcodeId", streetcodeIndex.Name);
            Assert.Equal("StreetcodeId", streetcodeIndex.Columns.Single());
        }

        [Fact]
        public void Down_ShouldDropCommentsTable()
        {
            var migration = new TestableAddCommentsMigration();
            var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

            migration.ApplyDown(migrationBuilder);

            var dropTable = Assert.Single(
                migrationBuilder.Operations.OfType<DropTableOperation>());

            Assert.Equal("comments", dropTable.Name);
            Assert.Equal("streetcode", dropTable.Schema);
        }

        private sealed class TestableAddCommentsMigration : AddComments
        {
            public void ApplyUp(MigrationBuilder migrationBuilder)
            {
                base.Up(migrationBuilder);
            }

            public void ApplyDown(MigrationBuilder migrationBuilder)
            {
                base.Down(migrationBuilder);
            }
        }
    }
}
