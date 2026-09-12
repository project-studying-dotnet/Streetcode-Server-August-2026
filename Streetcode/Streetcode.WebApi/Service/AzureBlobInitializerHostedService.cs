using Azure.Storage.Blobs;

namespace Streetcode.WebApi.Service;

public class AzureBlobInitializerHostedService : IHostedService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobInitializerHostedService(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync(
            Azure.Storage.Blobs.Models.PublicAccessType.None,
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
