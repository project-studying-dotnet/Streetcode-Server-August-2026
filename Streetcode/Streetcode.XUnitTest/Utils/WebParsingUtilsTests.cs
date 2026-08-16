using Streetcode.WebApi.Utils;
using Xunit;

namespace Streetcode.XUnitTest.Utils;

public class WebParsingUtilsTests
{
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
        var testDirectory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());
        try
        {
            string testFilePath = Path.Combine(testDirectory, "houses.csv");
            Directory.CreateDirectory(testDirectory);
            File.WriteAllText(testFilePath, " ");

            string result = WebParsingUtils.FindHousesCsv(testDirectory);

            Assert.Equal(testFilePath, result);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void FindHousesCsv_WhenFileExistsInNestedDirectory_ShouldReturnFilePath()
    {
        var testDirectory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());
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
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void FindHousesCsv_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var testDirectory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(testDirectory);

            var exception = Assert.Throws<FileNotFoundException>(
                () => WebParsingUtils.FindHousesCsv(testDirectory));

            Assert.Contains("houses.csv", exception.Message);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void FindHousesCsv_WhenFileNameHasDifferentCase_ShouldReturnFilePath()
    {
        var testDirectory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());
        try
        {
            Directory.CreateDirectory(testDirectory);
            var testFilePath = Path.Combine(testDirectory, "HOUSES.CSV");
            File.WriteAllText(testFilePath, "test");

            var result = WebParsingUtils.FindHousesCsv(testDirectory);

            Assert.Equal(testFilePath, result);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}
