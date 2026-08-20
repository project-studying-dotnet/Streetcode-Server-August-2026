using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Streetcode.BLL.DTO.Authentication;
using Streetcode.BLL.Interfaces.Jwt;
using Streetcode.DAL.Enums;

namespace Streetcode.BLL.Services.Jwt
{
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JsonWebTokenHandler _tokenHandler;

        public JwtService(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
            _tokenHandler = new JsonWebTokenHandler();
        }

        public AuthTokenResult GenerateToken(int userId, string email, UserRole role)
        {
            string secretKey = _jwtOptions.SecretKey;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_jwtOptions.LifetimeInMinutes);

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(ClaimTypes.Role, role.ToString()),
                }),
                Expires = expires,
                SigningCredentials = credentials,
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                IssuedAt = now
            };

            string token = _tokenHandler.CreateToken(tokenDescription);

            return new AuthTokenResult(token, expires);
        }
    }
}
