using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Streetcode.BLL.Services.Jwt;
using Streetcode.DAL.Enums;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Streetcode.XUnitTest.Services.Jwt;

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

    [Fact]
    public void GenerateToken_ValidInput_ReturnsTokenWithCorrectExpiration()
    {
        var before = DateTime.UtcNow;
        var result = _jwtService.GenerateToken(1, "admin@streetcode.ua", UserRole.MainAdministrator);
        var after = DateTime.UtcNow;

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        Assert.InRange(
        result.Expiration,
        before.AddMinutes(_jwtOptions.LifetimeInMinutes).AddSeconds(-2),
        after.AddMinutes(_jwtOptions.LifetimeInMinutes).AddSeconds(2));
    }

    [Fact]
    public void GenerateToken_ValidInput_TokenContainsCorrectClaimsAndMetadata()
    {
        int userId = 1;
        string email = "user@streetcode.ua";
        var role = UserRole.Administrator;

        var result = _jwtService.GenerateToken(userId, email, role);
        var token = _jwtHandler.ReadJsonWebToken(result.Token);

        Assert.Equal(userId.ToString(), token.GetClaim(JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(email, token.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(role.ToString(), token.GetClaim(ClaimTypes.Role).Value);

        Assert.True(Guid.TryParse(token.Id, out _));
    }

    [Theory]
    [InlineData(UserRole.MainAdministrator)]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Moderator)]
    public void GenerateToken_DifferentRoles_SetsCorrectRoleClaim(UserRole role)
    {
        var result = _jwtService.GenerateToken(10, "test@streetcode.ua", role);
        var token = _jwtHandler.ReadJsonWebToken(result.Token);

        var roleClaim = token.GetClaim(ClaimTypes.Role);

        Assert.NotNull(roleClaim);
        Assert.Equal(role.ToString(), roleClaim.Value);
    }

    [Fact]
    public void TEMP_PrintToken()
    {
        var result = _jwtService.GenerateToken(1, "admin@streetcode.ua", UserRole.MainAdministrator);
        Console.WriteLine(result.Token);
    }
}
