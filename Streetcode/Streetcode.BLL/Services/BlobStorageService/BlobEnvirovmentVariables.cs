namespace Streetcode.BLL.Services.BlobStorageService;

public class BlobEnvironmentVariables
{
    public string Provider { get; set; } = "Local";
    public string BlobStoreKey { get; set; }
    public string BlobStorePath { get; set; }
    public AzureBlobEnvironmentVariables Azure { get; set; } = new();
}