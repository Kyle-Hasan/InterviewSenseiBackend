using System;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;
using Microsoft.IdentityModel.Tokens;

namespace tests
{
    public static class JwtSettings
    {
        public static string SecretKey = "mySuperSecretKeyWhichIsLongEnoughForHmac2132324234232343mySuperSecretKeyWhichIsLongEnoughForHmac2132324234232343mySuperSecretKeyWhichIsLongEnoughForHmac2132324234232343";
        public static string RefreshTokenExpirationDays = "1440";
        public static string AccessTokenExpirationMinutes = "15";
    }

    public class AppUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }

    public interface IJwtTokenService
    {
        Task<string> GenerateToken(AppUser user, bool refreshToken);
        int ValidateRefreshToken(string token);
    }

    public class JwtTokenService : IJwtTokenService
    {
        public JwtTokenService() { }

        public async Task<string> GenerateToken(AppUser user, bool refreshToken)
        {
            var claims = new System.Collections.Generic.List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new("tokenType", refreshToken ? "refresh" : "access")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            DateTime expiration = refreshToken
                ? DateTime.Now.AddMinutes(int.Parse(JwtSettings.RefreshTokenExpirationDays))
                : DateTime.Now.AddMinutes(int.Parse(JwtSettings.AccessTokenExpirationMinutes));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int ValidateRefreshToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    var tokenType = jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value;
                    int? userId = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
                    if (userId.HasValue)
                    {
                        return userId.Value;
                    }
                    else
                    {
                        return -1;
                    }
                    if (tokenType == null || tokenType != "refresh")
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
            catch (SecurityTokenExpiredException)
            {
                return -1;
            }
            catch (Exception)
            {
                return -1;
            }

            return -1;
        }
    }

    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _service;

        public JwtTokenServiceTests()
        {
            _service = new JwtTokenService();
        }

        [Fact]
        public async Task GenerateToken_Returns_AccessToken_With_Correct_Claims()
        {
            var user = new AppUser { Id = 1, UserName = "testuser" };
            var token = await _service.GenerateToken(user, false);
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal("access", jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value);
            Assert.Equal("1", jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public async Task GenerateToken_Returns_RefreshToken_With_Correct_Claims()
        {
            var user = new AppUser { Id = 2, UserName = "testuser2" };
            var token = await _service.GenerateToken(user, true);
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            Assert.Equal("refresh", jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value);
            Assert.Equal("2", jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public async Task ValidateRefreshToken_Returns_UserId_For_Valid_RefreshToken()
        {
            var user = new AppUser { Id = 3, UserName = "refreshUser" };
            var token = await _service.GenerateToken(user, true);
            var result = _service.ValidateRefreshToken(token);
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task ValidateRefreshToken_Returns_MinusOne_For_AccessToken()
        {
            var user = new AppUser { Id = 4, UserName = "accessUser" };
            var token = await _service.GenerateToken(user, false);
            var result = _service.ValidateRefreshToken(token);
            Assert.Equal(-1, result);
        }

        [Fact]
        public void ValidateRefreshToken_Returns_MinusOne_For_InvalidToken()
        {
            var result = _service.ValidateRefreshToken("invalid.token");
            Assert.Equal(-1, result);
        }
    }
}
