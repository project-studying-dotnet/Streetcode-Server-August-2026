using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.BLL.Services.Jwt
{
    public class JwtOptions
    {
        public const string SectionName = "JwtOptions";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int LifetimeInMinutes { get; set; }
        public string SecretKey { get; set; } = string.Empty;
    }
}
