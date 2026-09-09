using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Streetcode.BLL.Services.BlobStorageService;
using Streetcode.DAL.Entities.Media;
using Streetcode.DAL.Entities.Media.Images;
using Streetcode.DAL.Repositories.Interfaces.Base;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.Services.BlobStorageService;

public class AzureBlobServiceTests
{
    private readonly Mock<BlobContainerClient> _containerClientMock;
    private readonly Mock<IRepositoryWrapper> _repositoryWrapperMock;
    private readonly AzureBlobService _service;

    public AzureBlobServiceTests()
    {
        _containerClientMock = new Mock<BlobContainerClient>();
        _repositoryWrapperMock = new Mock<IRepositoryWrapper>();

        _service = new AzureBlobService(_containerClientMock.Object, _repositoryWrapperMock.Object);
    }

    [Fact]
    public void DeleteFileInStorage_CallsDeleteIfExistsOnCorrectBlob()
    {
        var blobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/myblob.png"), new BlobClientOptions());
        _containerClientMock
            .Setup(c => c.GetBlobClient("myblob.png"))
            .Returns(blobClientMock.Object);

        _service.DeleteFileInStorage("myblob.png");

        blobClientMock.Verify(
            c => c.DeleteIfExists(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void FindFileInStorageAsBase64_ReturnsCorrectBase64_WhenBlobExists()
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes("hello world");
        var downloadResult = BlobsModelFactory.BlobDownloadResult(content: BinaryData.FromBytes(expectedBytes));
        var response = Response.FromValue(downloadResult, Mock.Of<Response>());

        var blobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/myblob.png"), new BlobClientOptions());
        blobClientMock
            .Setup(c => c.DownloadContent())
            .Returns(response);

        _containerClientMock
            .Setup(c => c.GetBlobClient("myblob.png"))
            .Returns(blobClientMock.Object);

        string result = _service.FindFileInStorageAsBase64("myblob.png");

        Assert.Equal(Convert.ToBase64String(expectedBytes), result);
    }

    [Fact]
    public void FindFileInStorageAsBase64_ThrowsFileNotFoundException_WhenBlobMissing()
    {
        var blobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/missing.png"), new BlobClientOptions());
        blobClientMock
            .Setup(c => c.DownloadContent())
            .Throws(new RequestFailedException(status: 404, message: "The specified blob does not exist."));

        _containerClientMock
            .Setup(c => c.GetBlobClient("missing.png"))
            .Returns(blobClientMock.Object);

        Assert.Throws<FileNotFoundException>(() => _service.FindFileInStorageAsBase64("missing.png"));
    }

    [Fact]
    public void FindFileInStorageAsMemoryStream_ReturnsCorrectStream_WhenBlobExists()
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes("hello stream");
        var downloadResult = BlobsModelFactory.BlobDownloadResult(content: BinaryData.FromBytes(expectedBytes));
        var response = Response.FromValue(downloadResult, Mock.Of<Response>());

        var blobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/audio.mp3"), new BlobClientOptions());
        blobClientMock
            .Setup(c => c.DownloadContent())
            .Returns(response);

        _containerClientMock
            .Setup(c => c.GetBlobClient("audio.mp3"))
            .Returns(blobClientMock.Object);

        MemoryStream result = _service.FindFileInStorageAsMemoryStream("audio.mp3");

        Assert.Equal(expectedBytes, result.ToArray());
    }

    [Fact]
    public void SaveFileInStorage_ReturnsNonEmptyHash_AndUploadsWithCorrectContentType()
    {
        var contentInfo = BlobsModelFactory.BlobContentInfo(
            eTag: new ETag("test-etag"),
            lastModified: DateTimeOffset.UtcNow,
            contentHash: Array.Empty<byte>(),
            versionId: null,
            encryptionKeySha256: null,
            encryptionScope: null,
            blobSequenceNumber: 0);
        var uploadResponse = Response.FromValue(contentInfo, Mock.Of<Response>());

        var blobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/whatever.png"), new BlobClientOptions());
        blobClientMock
            .Setup(c => c.Upload(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .Returns(uploadResponse);

        _containerClientMock
            .Setup(c => c.GetBlobClient(It.Is<string>(name => name.EndsWith(".png"))))
            .Returns(blobClientMock.Object);

        string hash = _service.SaveFileInStorage(
            base64: Convert.ToBase64String(Encoding.UTF8.GetBytes("fake image bytes")),
            name: "my-image",
            extension: "png");

        Assert.False(string.IsNullOrWhiteSpace(hash));

        blobClientMock.Verify(
            c => c.Upload(
                It.IsAny<Stream>(),
                It.Is<BlobUploadOptions>(o => o.HttpHeaders.ContentType == "image/png"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanBlobStorage_DeletesOnlyBlobsNotPresentInDatabase()
    {
        var blobItems = new List<BlobItem>
        {
            BlobsModelFactory.BlobItem(name: "used.png"),
            BlobsModelFactory.BlobItem(name: "orphan.png"),
        };
        _containerClientMock
            .Setup(c => c.GetBlobs())
            .Returns(new TestPageable<BlobItem>(blobItems));

        _repositoryWrapperMock
            .Setup(r => r.ImageRepository.GetAllAsync(null, null))
            .ReturnsAsync(new List<Image> { new Image { BlobName = "used.png" } });
        _repositoryWrapperMock
            .Setup(r => r.AudioRepository.GetAllAsync(null, null))
            .ReturnsAsync(new List<Audio>());

        var usedBlobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/used.png"), new BlobClientOptions());
        var orphanBlobClientMock = new Mock<BlobClient>(new Uri("https://test.blob.core.windows.net/container/orphan.png"), new BlobClientOptions());
        _containerClientMock.Setup(c => c.GetBlobClient("used.png")).Returns(usedBlobClientMock.Object);
        _containerClientMock.Setup(c => c.GetBlobClient("orphan.png")).Returns(orphanBlobClientMock.Object);

        await _service.CleanBlobStorage();

        orphanBlobClientMock.Verify(
            c => c.DeleteIfExists(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Once);
        usedBlobClientMock.Verify(
            c => c.DeleteIfExists(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class TestPageable<T> : Pageable<T>
        where T : notnull
    {
        private readonly IReadOnlyList<T> _items;

        public TestPageable(IReadOnlyList<T> items)
        {
            _items = items;
        }

        public override IEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            yield return Page<T>.FromValues(_items, null, Mock.Of<Response>());
        }
    }
}