// <copyright file="WebParsingUtilsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.Utils
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Streetcode.DAL.Entities.AdditionalContent.Coordinates.Types;
    using Streetcode.DAL.Entities.Toponyms;
    using Streetcode.DAL.Persistence;
    using Streetcode.WebApi.Utils;
    using Xunit;

    public class WebParsingUtilsTests
    {
        private const string CsvHeader =
            "region;old;new;gromada;community;unused;street;latitude;longitude";

        [Theory]
        [InlineData("region;old;new;gromada;community;unused;street;50.5;30.5")]
        [InlineData("region;old;new;gromada;community;unused;street;;30.5")]
        [InlineData("region;old;new;gromada;community;unused;street;not-a-number;30.5")]
        public async Task SaveToponymsToDbAsync_WithIncompleteOrInvalidDownload_DoesNotClearDatabase(
            string invalidRow)
        {
            string runtimeDirectory = CreateTemporaryDirectory();
            string csvPath = Path.Combine(runtimeDirectory, "data.csv");
            var csvRows = new[]
            {
                CsvHeader,
                invalidRow,
            };
            await File.WriteAllLinesAsync(csvPath, csvRows);

            var options = CreateDbContextOptions();

            try
            {
                await using var context = new StreetcodeDbContext(options);
                context.Toponyms.Add(new Toponym
                {
                    Oblast = "Existing oblast",
                    StreetName = "Existing toponym",
                    Coordinate = new ToponymCoordinate(),
                });
                context.Toponyms.Add(new Toponym
                {
                    Oblast = "Second existing oblast",
                    StreetName = "Second existing toponym",
                    Coordinate = new ToponymCoordinate(),
                });
                await context.SaveChangesAsync();
                var sut = CreateWebParsingUtils(context, runtimeDirectory);

                bool result = await sut.SaveToponymsToDbAsync(csvPath);

                Assert.False(result);
                Assert.Equal(2, await context.Toponyms.CountAsync());
                Assert.Contains(
                    await context.Toponyms.ToListAsync(),
                    x => x.StreetName == "Existing toponym");
            }
            finally
            {
                DeleteDirectory(runtimeDirectory);
            }
        }

        [Fact]
        public async Task ProcessCsvFileAsync_WhenParsingIsComplete_ShouldSaveToponyms()
        {
            string runtimeDirectory = CreateTemporaryDirectory();
            string csvPath = Path.Combine(runtimeDirectory, "data.csv");
            string housesCsvPath = Path.Combine(runtimeDirectory, "houses.csv");
            string[] csvRows =
            {
                CsvHeader,
                "region;old;new;gromada;community;unused;street;50.5;30.5",
            };
            await File.WriteAllLinesAsync(csvPath, csvRows);
            await File.WriteAllLinesAsync(housesCsvPath, csvRows);

            var options = CreateDbContextOptions();

            try
            {
                await using var context = new StreetcodeDbContext(options);
                var sut = CreateWebParsingUtils(context, runtimeDirectory);

                await sut.ProcessCsvFileAsync(runtimeDirectory);

                var savedToponym = Assert.Single(await context.Toponyms.ToListAsync());
                Assert.Equal("region", savedToponym.Oblast);
                Assert.Equal(50.5m, savedToponym.Coordinate.Latitude);
                Assert.Equal(30.5m, savedToponym.Coordinate.Longtitude);
            }
            finally
            {
                DeleteDirectory(runtimeDirectory);
            }
        }

        [Fact]
        public async Task SaveToponymsToDbAsync_WithValidData_ShouldReplaceExistingToponyms()
        {
            string runtimeDirectory = CreateTemporaryDirectory();
            string csvPath = Path.Combine(runtimeDirectory, "data.csv");
            string[] csvRows =
            {
                CsvHeader,
                "New region;old;new;gromada;community;unused;street;49.8;24.0",
            };
            await File.WriteAllLinesAsync(csvPath, csvRows);

            var options = CreateDbContextOptions();

            try
            {
                await using var context = new StreetcodeDbContext(options);
                context.Toponyms.Add(new Toponym
                {
                    Oblast = "Old region",
                    StreetName = "Old street",
                    Coordinate = new ToponymCoordinate(),
                });
                await context.SaveChangesAsync();
                var sut = CreateWebParsingUtils(context, runtimeDirectory);

                bool result = await sut.SaveToponymsToDbAsync(csvPath);

                Assert.True(result);
                var savedToponym = Assert.Single(await context.Toponyms.ToListAsync());
                Assert.Equal("New region", savedToponym.Oblast);
                Assert.Equal(49.8m, savedToponym.Coordinate.Latitude);
                Assert.Equal(24.0m, savedToponym.Coordinate.Longtitude);
            }
            finally
            {
                DeleteDirectory(runtimeDirectory);
            }
        }

        [Fact]
        public async Task ProcessCsvFileAsync_WhenDataFileIsMissing_ShouldInitializeItWithHeader()
        {
            string runtimeDirectory = CreateTemporaryDirectory();
            string csvPath = Path.Combine(runtimeDirectory, "data.csv");
            string housesCsvPath = Path.Combine(runtimeDirectory, "houses.csv");
            await File.WriteAllLinesAsync(housesCsvPath, new[] { CsvHeader });

            var options = CreateDbContextOptions();

            try
            {
                await using var context = new StreetcodeDbContext(options);
                var sut = CreateWebParsingUtils(context, runtimeDirectory);

                await sut.ProcessCsvFileAsync(runtimeDirectory);

                string savedHeader = Assert.Single(await File.ReadAllLinesAsync(csvPath));
                Assert.Equal(CsvHeader, savedHeader);
                Assert.Empty(await context.Toponyms.ToListAsync());
            }
            finally
            {
                DeleteDirectory(runtimeDirectory);
            }
        }

        [Fact]
        public async Task ProcessCsvFileAsync_WhenParsingIsIncomplete_ShouldKeepExistingToponyms()
        {
            string runtimeDirectory = CreateTemporaryDirectory();
            string csvPath = Path.Combine(runtimeDirectory, "data.csv");
            string housesCsvPath = Path.Combine(runtimeDirectory, "houses.csv");
            const string parsedHeader =
                "parsed;old;new;gromada;community;unused;street;latitude;longitude";
            const string sourceHeader =
                "source;old;new;gromada;community;unused;street;latitude;longitude";
            const string validRow =
                "region;old;new;gromada;community;unused;street;50.5;30.5";
            await File.WriteAllLinesAsync(csvPath, new[] { parsedHeader, validRow });
            await File.WriteAllLinesAsync(housesCsvPath, new[] { sourceHeader, validRow });

            var options = CreateDbContextOptions();

            try
            {
                await using var context = new StreetcodeDbContext(options);
                context.Toponyms.Add(new Toponym
                {
                    Oblast = "Existing region",
                    StreetName = "Existing street",
                    Coordinate = new ToponymCoordinate(),
                });
                await context.SaveChangesAsync();
                var sut = CreateWebParsingUtils(context, runtimeDirectory);

                await sut.ProcessCsvFileAsync(runtimeDirectory);

                var existingToponym = Assert.Single(await context.Toponyms.ToListAsync());
                Assert.Equal("Existing region", existingToponym.Oblast);
            }
            finally
            {
                DeleteDirectory(runtimeDirectory);
            }
        }

        [Fact]
        public async Task SaveToponymsToDbAsync_WhenSaveFails_ShouldRollbackAndRethrow()
        {
            string runtimeDirectory = CreateTemporaryDirectory();
            string csvPath = Path.Combine(runtimeDirectory, "data.csv");
            string[] csvRows =
            {
                CsvHeader,
                "region;old;new;gromada;community;unused;street;50.5;30.5",
            };
            await File.WriteAllLinesAsync(csvPath, csvRows);

            var options = CreateDbContextOptions();

            try
            {
                await using var context = new FailingStreetcodeDbContext(options);
                context.ShouldFailOnSave = true;
                var sut = CreateWebParsingUtils(context, runtimeDirectory);

                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => sut.SaveToponymsToDbAsync(csvPath));
            }
            finally
            {
                DeleteDirectory(runtimeDirectory);
            }
        }

        [Fact]
        public void GetZipPath_WhenCalled_ShouldReturnPathInsideTemporaryDirectory()
        {
            string temporaryDirectory = Path.GetTempPath();
            Guid operationId = Guid.Parse("12345678-1234-1234-1234-123456789012");
            string expectedPath = Path.Combine(temporaryDirectory, $"houses-{operationId}.zip");

            string result = WebParsingUtils.GetZipPath(temporaryDirectory, operationId);

            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void GetZipPath_WithDifferentOperationIds_ShouldReturnDifferentPaths()
        {
            string temporaryDirectory = Path.GetTempPath();
            Guid firstOperationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid secondOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            string firstResult = WebParsingUtils.GetZipPath(temporaryDirectory, firstOperationId);
            string secondResult = WebParsingUtils.GetZipPath(temporaryDirectory, secondOperationId);

            Assert.NotEqual(firstResult, secondResult);
        }

        [Fact]
        public void GetDataDirectory_WhenCalled_ShouldReturnDirectoryUnderContentRoot()
        {
            string contentRootPath = Path.GetTempPath();
            string expectedPath = Path.Combine(contentRootPath, "Data", "WebParsing");

            string result = WebParsingUtils.GetDataDirectory(contentRootPath);

            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void FindHousesCsv_WhenFileExistsInRoot_ShouldReturnFilePath()
        {
            string testDirectory = CreateTemporaryDirectory();
            try
            {
                string testFilePath = Path.Combine(testDirectory, "houses.csv");
                File.WriteAllText(testFilePath, " ");

                string result = WebParsingUtils.FindHousesCsv(testDirectory);

                Assert.Equal(testFilePath, result);
            }
            finally
            {
                DeleteDirectory(testDirectory);
            }
        }

        [Fact]
        public void FindHousesCsv_WhenFileExistsInNestedDirectory_ShouldReturnFilePath()
        {
            string testDirectory = CreateTemporaryDirectory();
            try
            {
                var nestedDirectory = Path.Combine(testDirectory, "Nested");
                Directory.CreateDirectory(nestedDirectory);
                string testFilePath = Path.Combine(nestedDirectory, "houses.csv");
                File.WriteAllText(testFilePath, " ");

                string result = WebParsingUtils.FindHousesCsv(testDirectory);

                Assert.Equal(testFilePath, result);
            }
            finally
            {
                DeleteDirectory(testDirectory);
            }
        }

        [Fact]
        public void FindHousesCsv_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            string testDirectory = CreateTemporaryDirectory();
            try
            {
                var exception = Assert.Throws<FileNotFoundException>(
                    () => WebParsingUtils.FindHousesCsv(testDirectory));

                Assert.Contains("houses.csv", exception.Message);
            }
            finally
            {
                DeleteDirectory(testDirectory);
            }
        }

        [Fact]
        public void FindHousesCsv_WhenFileNameHasDifferentCase_ShouldReturnFilePath()
        {
            string testDirectory = CreateTemporaryDirectory();
            try
            {
                var testFilePath = Path.Combine(testDirectory, "HOUSES.CSV");
                File.WriteAllText(testFilePath, "test");

                var result = WebParsingUtils.FindHousesCsv(testDirectory);

                Assert.Equal(testFilePath, result);
            }
            finally
            {
                DeleteDirectory(testDirectory);
            }
        }

        private static string CreateTemporaryDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static DbContextOptions<StreetcodeDbContext> CreateDbContextOptions()
        {
            return new DbContextOptionsBuilder<StreetcodeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(configuration =>
                    configuration.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        private static WebParsingUtils CreateWebParsingUtils(
            StreetcodeDbContext context,
            string runtimeDirectory)
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(runtimeDirectory);
            environment.SetupGet(x => x.ContentRootFileProvider).Returns(Mock.Of<IFileProvider>());
            return new WebParsingUtils(context, environment.Object);
        }

        private static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private sealed class FailingStreetcodeDbContext : StreetcodeDbContext
        {
            public FailingStreetcodeDbContext(
                DbContextOptions<StreetcodeDbContext> options)
                : base(options)
            {
            }

            public bool ShouldFailOnSave { get; set; }

            public override Task<int> SaveChangesAsync(
                CancellationToken cancellationToken = default)
            {
                if (this.ShouldFailOnSave)
                {
                    throw new InvalidOperationException("Test save failure.");
                }

                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
