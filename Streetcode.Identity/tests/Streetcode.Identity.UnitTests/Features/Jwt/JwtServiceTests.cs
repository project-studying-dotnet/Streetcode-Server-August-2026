using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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
        private readonly DateTimeOffset utcNow;

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
            this.utcNow = TimeProvider.System.GetUtcNow();

            this.jwtService = new JwtService(optionsWrapper, new FixedTimeProvider(this.utcNow));
            this.jwtHandler = new JsonWebTokenHandler();
        }

        [Fact]
        public void GenerateToken_ValidInput_ReturnsTokenWithCorrectExpiration()
        {
            var result = this.jwtService.GenerateToken(
                Guid.NewGuid(), "admin@streetcode.ua", new[] { "TestRole" }, accessVersion: 1);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.Equal(
                this.utcNow.UtcDateTime.AddMinutes(this.jwtOptions.LifetimeInMinutes),
                result.Expiration);
        }

        [Fact]
        public void GenerateToken_ValidInput_TokenContainsCorrectClaimsAndMetadata()
        {
            Guid userId = Guid.NewGuid();
            string email = "user@streetcode.ua";
            var roles = new[] { "TestRole" };
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
        [InlineData("TestRoleA")]
        [InlineData("TestRoleB")]
        [InlineData("TestRoleC")]
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
            var roles = new[] { "TestRoleA", "TestRoleB" };

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
                Guid.NewGuid(), "test@streetcode.ua", new[] { "TestRole" }, accessVersion);
            var token = this.jwtHandler.ReadJsonWebToken(result.Token);

            Assert.Equal(accessVersion.ToString(), token.GetClaim("access_version").Value);
        }

        [Fact]
        public async Task ValidateTokenAsync_ValidToken_ReturnsValidResult()
        {
            var tokenResult = this.jwtService.GenerateToken(
                Guid.NewGuid(),
                "test@streetcode.ua",
                new[] { "TestRole" },
                accessVersion: 1);

            var validationResult = await this.jwtHandler.ValidateTokenAsync(
                tokenResult.Token,
                this.CreateTokenValidationParameters());

            Assert.True(validationResult.IsValid);
            Assert.Null(validationResult.Exception);
        }

        [Fact]
        public async Task ValidateTokenAsync_WrongSigningKey_ReturnsInvalidResult()
        {
            const string wrongSecretKey =
                "Different_Secret_Key_For_Testing_At_Least_32_Characters!";

            var tokenResult = this.jwtService.GenerateToken(
                Guid.NewGuid(),
                "test@streetcode.ua",
                Array.Empty<string>(),
                accessVersion: 1);

            var validationResult = await this.jwtHandler.ValidateTokenAsync(
                tokenResult.Token,
                this.CreateTokenValidationParameters(secretKey: wrongSecretKey));

            Assert.False(validationResult.IsValid);
            Assert.NotNull(validationResult.Exception);
            Assert.IsType<SecurityTokenSignatureKeyNotFoundException>(
                validationResult.Exception);
        }

        [Fact]
        public async Task ValidateTokenAsync_WrongIssuer_ReturnsInvalidResult()
        {
            const string wrongIssuer = "DifferentTestIssuer";

            var tokenResult = this.jwtService.GenerateToken(
                Guid.NewGuid(),
                "test@streetcode.ua",
                Array.Empty<string>(),
                accessVersion: 1);

            var validationResult = await this.jwtHandler.ValidateTokenAsync(
                tokenResult.Token,
                this.CreateTokenValidationParameters(issuer: wrongIssuer));

            Assert.False(validationResult.IsValid);
            Assert.NotNull(validationResult.Exception);
            Assert.IsType<SecurityTokenInvalidIssuerException>(
                validationResult.Exception);
        }

        [Fact]
        public async Task ValidateTokenAsync_WrongAudience_ReturnsInvalidResult()
        {
            const string wrongAudience = "DifferentTestAudience";

            var tokenResult = this.jwtService.GenerateToken(
                Guid.NewGuid(),
                "test@streetcode.ua",
                Array.Empty<string>(),
                accessVersion: 1);

            var validationResult = await this.jwtHandler.ValidateTokenAsync(
                tokenResult.Token,
                this.CreateTokenValidationParameters(audience: wrongAudience));

            Assert.False(validationResult.IsValid);
            Assert.NotNull(validationResult.Exception);
            Assert.IsType<SecurityTokenInvalidAudienceException>(
                validationResult.Exception);
        }

        [Fact]
        public async Task ValidateTokenAsync_ExpiredToken_ReturnsInvalidResult()
        {
            var tokenIssuedAt = TimeProvider.System.GetUtcNow()
                .AddMinutes(-(this.jwtOptions.LifetimeInMinutes + 1));
            var expiredJwtService = new JwtService(
                Options.Create(this.jwtOptions),
                new FixedTimeProvider(tokenIssuedAt));

            var tokenResult = expiredJwtService.GenerateToken(
                Guid.NewGuid(),
                "test@streetcode.ua",
                Array.Empty<string>(),
                accessVersion: 1);

            var validationResult = await this.jwtHandler.ValidateTokenAsync(
                tokenResult.Token,
                this.CreateTokenValidationParameters());

            Assert.False(validationResult.IsValid);
            Assert.NotNull(validationResult.Exception);
            Assert.IsType<SecurityTokenExpiredException>(
                validationResult.Exception);
        }

        private TokenValidationParameters CreateTokenValidationParameters(
            string? secretKey = null,
            string? issuer = null,
            string? audience = null)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey ?? this.jwtOptions.SecretKey)),

                ValidateIssuer = true,
                ValidIssuer = issuer ?? this.jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = audience ?? this.jwtOptions.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.Zero,

                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            };
        }

        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset utcNow;

            public FixedTimeProvider(DateTimeOffset utcNow)
            {
                this.utcNow = utcNow;
            }

            public override DateTimeOffset GetUtcNow() => this.utcNow;
        }
    }
}
