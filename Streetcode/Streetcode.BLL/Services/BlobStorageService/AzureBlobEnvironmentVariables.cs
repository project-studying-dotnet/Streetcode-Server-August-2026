using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.BLL.Services.BlobStorageService
{
    public class AzureBlobEnvironmentVariables
    {
        public string ConnectionString { get; set; }

        public string ContainerName { get; set; }
    }
}
