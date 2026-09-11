using System;
using System.Collections.Generic;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Streetcode.BLL.Interfaces.BlobStorage;
using Streetcode.BLL.Util;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.Services.BlobStorageService;

public class AzureBlobService : IBlobService
{
    private readonly BlobContainerClient _containerClient;
    private readonly IRepositoryWrapper _repositoryWrapper;

    public AzureBlobService(
        BlobContainerClient blobServiceClient,
        IRepositoryWrapper? repositoryWrapper = null)
    {
        _containerClient = blobServiceClient;
        _repositoryWrapper = repositoryWrapper;
    }

    public void DeleteFileInStorage(string name)
    {
        var client = _containerClient.GetBlobClient(name);

        client.DeleteIfExists();
    }

    public string FindFileInStorageAsBase64(string name)
    {
        byte[] bytes = DownloadBytes(name);

        return Convert.ToBase64String(bytes);
    }

    public MemoryStream FindFileInStorageAsMemoryStream(string name)
    {
        byte[] bytes = DownloadBytes(name);

        return new MemoryStream(bytes);
    }

    public string SaveFileInStorage(string base64, string name, string extension)
    {
        var hashBlobStorageName = BlobHelper.GetHashedFileName(name);
        byte[] imageBytes = Convert.FromBase64String(base64);

        extension = BlobHelper.NormalizeExtension(extension);

        var blobName = $"{hashBlobStorageName}.{extension}";

        BlobClient client = _containerClient.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = GetContentType(extension) }
        };

        client.Upload(new MemoryStream(imageBytes), options);

        return blobName;
    }

    public void SaveFileInStorageBase64(string base64, string name, string extension)
    {
        byte[] imageBytes = Convert.FromBase64String(base64);

        extension = BlobHelper.NormalizeExtension(extension);

        BlobClient client = _containerClient.GetBlobClient($"{name}.{extension}");

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = GetContentType(extension) }
        };

        client.Upload(new MemoryStream(imageBytes), options);
    }

    public string UpdateFileInStorage(string previousBlobName, string base64Format, string newBlobName, string extension)
    {
        string blobName = SaveFileInStorage(base64Format, newBlobName, extension);

        DeleteFileInStorage(previousBlobName);

        return blobName;
    }

    public async Task CleanBlobStorage()
    {
        var blobNames = _containerClient.GetBlobs().Select(b => b.Name).ToList();

        var existingImages = await _repositoryWrapper.ImageRepository.GetAllAsync();
        var existingAudios = await _repositoryWrapper.AudioRepository.GetAllAsync();

        List<string> existingMedia = new();

        existingMedia.AddRange(existingImages.Select(img => img.BlobName));
        existingMedia.AddRange(existingAudios.Select(a => a.BlobName));

        var filesToRemove = blobNames.Except(existingMedia).ToList();
        foreach (var file in filesToRemove)
        {
            DeleteFileInStorage(file);
        }
    }

    private byte[] DownloadBytes(string name)
    {
        var client = _containerClient.GetBlobClient(name);
        BlobDownloadResult downloadResult;
        try
        {
            downloadResult = client.DownloadContent();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"Blob '{name}' was not found in storage.", ex);
        }

        return downloadResult.Content.ToArray();
    }

    private static string GetContentType(string extension) => extension switch
    {
        "png" => "image/png",
        "jpg" or "jpeg" => "image/jpeg",
        "mp3" => "audio/mpeg",
        "gif" => "image/gif",
        _ => "application/octet-stream",
    };
}
