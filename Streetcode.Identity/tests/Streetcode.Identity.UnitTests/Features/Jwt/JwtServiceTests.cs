using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Streetcode.Identity.Infrastructure.Identity.Jwt;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Streetcode.Identity.UnitTests.Features.Jwt
{
    public class JwtServiceTests
    {
        private readonly JwtOptions jwtOptions;
        private readonly JwtService jwtService;
        private readonly JsonWebTokenHandler jwtHandler;

        public JwtServiceTests()
        {
            this.jwtOptions = new JwtOptions
            {
                Issuer = "StreetcodeTestIssuer",
                Audience = "StreetcodeTestAudience",
                LifetimeInMinutes = 60,
                SecretKey = "Super_Secret_Key_For_Testing_Purposes_32_Bytes_Long!",
            };

            var optionsWrapper = Options.Create(this.jwtOptions);

            this.jwtService = new JwtService(optionsWrapper);
            this.jwtHandler = new JsonWebTokenHandler();
        }

        [Fact]
        public void GenerateToken_ValidInput_ReturnsTokenWithCorrectExpiration()
        {
            var before = DateTime.UtcNow;
            var result = this.jwtService.GenerateToken(
                Guid.NewGuid(), "admin@streetcode.ua", new[] { "MainAdministrator" }, accessVersion: 1);
            var after = DateTime.UtcNow;

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            Assert.InRange(
                result.Expiration,
                before.AddMinutes(this.jwtOptions.LifetimeInMinutes).AddSeconds(-2),
                after.AddMinutes(this.jwtOptions.LifetimeInMinutes).AddSeconds(2));
        }

        [Fact]
        public void GenerateToken_ValidInput_TokenContainsCorrectClaimsAndMetadata()
        {
            Guid userId = Guid.NewGuid();
            string email = "user@streetcode.ua";
            var roles = new[] { "Administrator" };
            const long accessVersion = 1;

            var result = this.jwtService.GenerateToken(userId, email, roles, accessVersion);
            var token = this.jwtHandler.ReadJsonWebToken(result.Token);

            Assert.Equal(userId.ToString(), token.GetClaim(JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal(email, token.GetClaim(JwtRegisteredClaimNames.Email).Value);
            Assert.Equal(accessVersion.ToString(), token.GetClaim("access_version").Value);

            var roleClaims = token.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            Assert.Equal(roles, roleClaims);
            Assert.True(Guid.TryParse(token.Id, out _));
        }

        [Theory]
        [InlineData("MainAdministrator")]
        [InlineData("Administrator")]
        [InlineData("Moderator")]
        public void GenerateToken_SingleRole_SetsCorrectRoleClaim(string role)
        {
            var result = this.jwtService.GenerateToken(
                Guid.NewGuid(), "test@streetcode.ua", new[] { role }, accessVersion: 1);
            var token = this.jwtHandler.ReadJsonWebToken(result.Token);

            var roleClaims = token.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            Assert.Single(roleClaims);
            Assert.Equal(role, roleClaims[0]);
        }

        [Fact]
        public void GenerateToken_MultipleRoles_SetsAllRoleClaims()
        {
            var roles = new[] { "Administrator", "Moderator" };

            var result = this.jwtService.GenerateToken(
                Guid.NewGuid(), "test@streetcode.ua", roles, accessVersion: 1);
            var token = this.jwtHandler.ReadJsonWebToken(result.Token);

            var roleClaims = token.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            Assert.Equal(roles.Length, roleClaims.Count);
            Assert.All(roles, expectedRole => Assert.Contains(expectedRole, roleClaims));
        }

        [Fact]
        public void GenerateToken_EmptyRoles_ProducesNoRoleClaims()
        {
            var result = this.jwtService.GenerateToken(
                Guid.NewGuid(), "test@streetcode.ua", Array.Empty<string>(), accessVersion: 1);
            var token = this.jwtHandler.ReadJsonWebToken(result.Token);

            var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role);

            Assert.Empty(roleClaims);
        }

        [Fact]
        public void GenerateToken_IncludesAccessVersionClaim()
        {
            const long accessVersion = 42;

            var result = this.jwtService.GenerateToken(
                Guid.NewGuid(), "test@streetcode.ua", new[] { "User" }, accessVersion);
            var token = this.jwtHandler.ReadJsonWebToken(result.Token);

            Assert.Equal(accessVersion.ToString(), token.GetClaim("access_version").Value);
        }
    }
}
