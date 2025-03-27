using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API;
using API.Auth;
using API.Users;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

public class JwtTokenServiceTests
{
    private static readonly string TestSecretKey = "supersecretkeythaertetertetretretetertetretertertetetrtetisverylong";

    [Fact]
    public async void GenerateToken_AccessToken_ReturnsValidToken()
    {
        // Arrange
        var user = new AppUser { Id = 1, UserName = "testUser" };
        JwtSettings.SecretKey = TestSecretKey;
        JwtSettings.AccessTokenExpirationMinutes = "60";
        var service = new JwtTokenService();

        // Act
        var token = await service.GenerateToken(user, false);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal("access", jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value);
        Assert.Equal("1", jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("testUser", jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);
        Assert.True(jwtToken.ValidTo > DateTime.Now);
    }

    [Fact]
    public async void GenerateToken_RefreshToken_ReturnsValidToken()
    {
        // Arrange
        var user = new AppUser { Id = 1, UserName = "testUser" };
        JwtSettings.SecretKey = TestSecretKey;
        JwtSettings.RefreshTokenExpirationDays = "1";
        var service = new JwtTokenService();

        // Act
        var token = await service.GenerateToken(user, true);

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal("refresh", jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value);
        Assert.Equal("1", jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("testUser", jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);
        Assert.True(jwtToken.ValidTo > DateTime.Now);
    }

    [Fact]
    public void ValidateRefreshToken_ValidToken_ReturnsUserId()
    {
        // Arrange
        var user = new AppUser { Id = 1, UserName = "testUser" };
        JwtSettings.SecretKey = TestSecretKey;
        JwtSettings.RefreshTokenExpirationDays = "1";
        var service = new JwtTokenService();
        var token = service.GenerateToken(user, true).Result;

        // Act
        var userId = service.ValidateRefreshToken(token);

        // Assert
        Assert.Equal(1, userId);
    }

    [Fact]
    public void ValidateRefreshToken_InvalidToken_ReturnsNegativeOne()
    {
        // Arrange
        JwtSettings.SecretKey = TestSecretKey;
        var service = new JwtTokenService();
        var invalidToken = "invalidToken";

        // Act
        var userId = service.ValidateRefreshToken(invalidToken);

        // Assert
        Assert.Equal(-1, userId);
    }

    [Fact]
    public void ValidateRefreshToken_ExpiredToken_ReturnsNegativeOne()
    {
        // Arrange
        var user = new AppUser { Id = 1, UserName = "testUser" };
        JwtSettings.SecretKey = TestSecretKey;
        JwtSettings.RefreshTokenExpirationDays = "1";
        var service = new JwtTokenService();
        var token = service.GenerateToken(user, true).Result;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expiredToken = handler.WriteToken(jwtToken);

        // Act
        var userId = service.ValidateRefreshToken(expiredToken);

        // Assert
        Assert.Equal(-1, userId);
    }

    [Fact]
    public void ValidateRefreshToken_ExpiredAccessToken_ReturnsNegativeOne()
    {
        // Arrange
        var user = new AppUser { Id = 1, UserName = "testUser" };
        JwtSettings.SecretKey = TestSecretKey;
        JwtSettings.AccessTokenExpirationMinutes = "-60"; // Generate a token that's already expired
        var service = new JwtTokenService();
        var token = service.GenerateToken(user, false).Result;

        // Act
        var userId = service.ValidateRefreshToken(token);

        // Assert
        Assert.Equal(-1, userId);
    }

    [Fact]
    public void ValidateRefreshToken_MissingUserIdClaim_ReturnsNegativeOne()
    {
        // Arrange
        JwtSettings.SecretKey = TestSecretKey;
        var service = new JwtTokenService();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testUser"),
            new("tokenType", "refresh")
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var userId = service.ValidateRefreshToken(tokenString);

        // Assert
        Assert.Equal(-1, userId);
    }

    [Fact]
    public void ValidateRefreshToken_MissingTokenTypeClaim_ReturnsNegativeOne()
    {
        // Arrange
        JwtSettings.SecretKey = TestSecretKey;
        var service = new JwtTokenService();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var claims = new List<Claim>
        {
            
            new(ClaimTypes.Name, "testUser")
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var userId = service.ValidateRefreshToken(tokenString);

        // Assert
        Assert.Equal(-1, userId);
    }
}