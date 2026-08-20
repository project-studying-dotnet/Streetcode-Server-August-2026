using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Streetcode.BLL.Services.Jwt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streetcode.XUnitTest.Services.Jwt
{
    public class JwtServiceTests
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JwtService _jwtService;
        private readonly JsonWebTokenHandler _jwtHandler;

        public JwtServiceTests()
        {
            _jwtOptions = new JwtOptions
            {
                Issuer = "StreetcodeTestIssuer",
                Audience = "StreetcodeTestAudience",
                LifetimeInMinutes = 60,
                SecretKey = "Super_Secret_Key_For_Testing_Purposes_32_Bytes_Long!"
            };

            var optionsWrapper = Options.Create(_jwtOptions);

            _jwtService = new JwtService(optionsWrapper);
            _jwtHandler = new JsonWebTokenHandler();
        }
    }
}
