using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.BLL.Services.CacheService;

public class CacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 30;
}
