using Microsoft.EntityFrameworkCore;
using Streetcode.DAL.Entities.HistoryMap;
using Streetcode.DAL.Entities.Toponyms;
using Streetcode.DAL.Persistence;
using Streetcode.DAL.Repositories.Realizations.HistoryMap;
using Xunit;

namespace Streetcode.XUnitTest.MediatRTests.HistoryMap
{
    public class HistoryMapRecordRepositoryTests
    {
        [Fact]
        public async Task GetByStreetcodeIdAsync_ShouldReturnOnlyRecordsForStreetcodeOrderedByPhysicalNumber()
        {
            await using var context = CreateContext();

            var firstToponym = new Toponym
            {
                Id = 1,
                Oblast = "Kyiv",
                StreetName = "Main Street",
            };

            var secondToponym = new Toponym
            {
                Id = 2,
                Oblast = "Lviv",
                StreetName = "Stepan Bandera Street",
            };

            context.Toponyms.AddRange(firstToponym, secondToponym);

            context.HistoryMapRecords.AddRange(
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 1,
                    Toponym = firstToponym,
                    PhysicalStreetcodeNumber = 30,
                },
                new HistoryMapRecord
                {
                    Id = 2,
                    StreetcodeId = 10,
                    ToponymId = 2,
                    Toponym = secondToponym,
                    PhysicalStreetcodeNumber = 10,
                },
                new HistoryMapRecord
                {
                    Id = 3,
                    StreetcodeId = 11,
                    ToponymId = 1,
                    Toponym = firstToponym,
                    PhysicalStreetcodeNumber = 5,
                });

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            var result = (await repository.GetByStreetcodeIdAsync(10)).ToList();

            Assert.Equal(2, result.Count);

            Assert.Equal(10, result[0].PhysicalStreetcodeNumber);
            Assert.Equal(30, result[1].PhysicalStreetcodeNumber);

            Assert.Equal(2, result[0].Id);
            Assert.Equal(1, result[1].Id);

            Assert.NotNull(result[0].Toponym);
            Assert.NotNull(result[1].Toponym);

            Assert.Equal("Stepan Bandera Street", result[0].Toponym!.StreetName);
            Assert.Equal("Main Street", result[1].Toponym!.StreetName);

            Assert.All(result, record => Assert.Equal(10, record.StreetcodeId));
        }

        [Fact]
        public async Task GetByStreetcodeIdAsync_WhenNoRecordsExist_ShouldReturnEmptyCollection()
        {
            await using var context = CreateContext();

            var repository = new HistoryMapRecordRepository(context);

            var result = await repository.GetByStreetcodeIdAsync(999);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByStreetcodeAndNumberAsync_WhenRecordExists_ShouldReturnExactRecord()
        {
            // Arrange
            await using var context = CreateContext();

            var expectedRecord = new HistoryMapRecord
            {
                Id = 1,
                StreetcodeId = 10,
                ToponymId = 20,
                PhysicalStreetcodeNumber = 42
            };

            context.HistoryMapRecords.Add(expectedRecord);

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            // Act
            var result = await repository.GetByStreetcodeAndNumberAsync(
                streetcodeId: 10,
                physicalStreetcodeNumber: 42);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedRecord.Id, result!.Id);
            Assert.Equal(expectedRecord.StreetcodeId, result.StreetcodeId);
            Assert.Equal(expectedRecord.ToponymId, result.ToponymId);
            Assert.Equal(
                expectedRecord.PhysicalStreetcodeNumber,
                result.PhysicalStreetcodeNumber);
        }

        [Fact]
        public async Task GetByStreetcodeAndNumberAsync_WhenPhysicalNumberDoesNotMatch_ShouldReturnNull()
        {
            // Arrange
            await using var context = CreateContext();

            context.HistoryMapRecords.Add(
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 20,
                    PhysicalStreetcodeNumber = 42
                });

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            // Act
            var result = await repository.GetByStreetcodeAndNumberAsync(
                streetcodeId: 10,
                physicalStreetcodeNumber: 43);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByStreetcodeAndNumberAsync_WhenStreetcodeDoesNotMatch_ShouldReturnNull()
        {
            // Arrange
            await using var context = CreateContext();

            context.HistoryMapRecords.Add(
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 20,
                    PhysicalStreetcodeNumber = 42
                });

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            // Act
            var result = await repository.GetByStreetcodeAndNumberAsync(
                streetcodeId: 11,
                physicalStreetcodeNumber: 42);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByToponymIdAsync_ShouldReturnOnlyRecordsForToponym()
        {
            // Arrange
            await using var context = CreateContext();

            context.HistoryMapRecords.AddRange(
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 100,
                    PhysicalStreetcodeNumber = 1
                },
                new HistoryMapRecord
                {
                    Id = 2,
                    StreetcodeId = 20,
                    ToponymId = 100,
                    PhysicalStreetcodeNumber = 2
                },
                new HistoryMapRecord
                {
                    Id = 3,
                    StreetcodeId = 30,
                    ToponymId = 200,
                    PhysicalStreetcodeNumber = 3
                });

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            // Act
            var result = (await repository.GetByToponymIdAsync(100)).ToList();

            // Assert
            Assert.Equal(2, result.Count);

            Assert.All(
                result,
                record => Assert.Equal(100, record.ToponymId));

            Assert.Contains(result, record => record.Id == 1);
            Assert.Contains(result, record => record.Id == 2);
            Assert.DoesNotContain(result, record => record.Id == 3);
        }

        [Fact]
        public async Task GetByToponymIdAsync_WhenNoRecordsExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            await using var context = CreateContext();

            context.HistoryMapRecords.Add(
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 100,
                    PhysicalStreetcodeNumber = 1
                });

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            // Act
            var result = await repository.GetByToponymIdAsync(999);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByToponymIdAsync_ShouldNotLoadStreetcodeNavigation()
        {
            // Arrange
            await using var context = CreateContext();

            context.HistoryMapRecords.Add(
                new HistoryMapRecord
                {
                    Id = 1,
                    StreetcodeId = 10,
                    ToponymId = 100,
                    PhysicalStreetcodeNumber = 1
                });

            await context.SaveChangesAsync();

            var repository = new HistoryMapRecordRepository(context);

            // Act
            var result = (await repository.GetByToponymIdAsync(100)).ToList();

            // Assert
            var record = Assert.Single(result);

            Assert.Null(record.Streetcode);
        }

        [Fact]
        public void Model_ShouldConfigureUniqueIndexForStreetcodeAndPhysicalNumber()
        {
            // Arrange
            using var context = CreateContext();

            var entityType = context.Model.FindEntityType(
                typeof(HistoryMapRecord));

            Assert.NotNull(entityType);

            // Act
            var uniqueIndex = entityType!
                .GetIndexes()
                .SingleOrDefault(index =>
                    index.IsUnique &&
                    index.Properties.Count == 2 &&
                    index.Properties[0].Name ==
                        nameof(HistoryMapRecord.StreetcodeId) &&
                    index.Properties[1].Name ==
                        nameof(HistoryMapRecord.PhysicalStreetcodeNumber));

            // Assert
            Assert.NotNull(uniqueIndex);
        }

        private static StreetcodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new StreetcodeDbContext(options);
        }
    }
}