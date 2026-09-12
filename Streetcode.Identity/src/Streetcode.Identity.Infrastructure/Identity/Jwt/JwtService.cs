using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Streetcode.Identity.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Streetcode.Identity.Infrastructure.Identity.Jwt
{
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JsonWebTokenHandler _tokenHandler;
        private readonly TimeProvider _timeProvider;

        public JwtService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _jwtOptions = jwtOptions.Value;
            _tokenHandler = new JsonWebTokenHandler();
        }

        public AuthTokenResult GenerateToken(Guid userId, string email, IEnumerable<string> roles, long accessVersion)
        {
            string secretKey = _jwtOptions.SecretKey;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var expires = now.AddMinutes(_jwtOptions.LifetimeInMinutes);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("access_version", accessVersion.ToString(), ClaimValueTypes.Integer64),
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = credentials,
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                IssuedAt = now,
                NotBefore = now
            };

            string token = _tokenHandler.CreateToken(tokenDescription);

            return new AuthTokenResult(token, expires);
        }
    }
}
