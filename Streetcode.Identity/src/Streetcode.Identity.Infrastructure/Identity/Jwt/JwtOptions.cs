using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.Identity.Infrastructure.Identity.Jwt
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int LifetimeInMinutes { get; set; }
        public string SecretKey { get; set; } = string.Empty;
    }
}
