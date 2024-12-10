using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API;
using API.Auth;
using API.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) {
        _config = config;
    }

    public async Task<string> GenerateToken(AppUser user, bool refreshToken) {

        JwtSettings jwtSettings = _config.GetSection("JwtSettings").Get<JwtSettings>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new(ClaimTypes.Name,user.UserName),
            new("tokenType", refreshToken ? "refresh" : "access")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha512);

        DateTime expiration = refreshToken ? DateTime.Now.AddMinutes(jwtSettings.RefreshTokenExpirationDays) : 
        DateTime.Now.AddMinutes(jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
        public  bool ValidateRefreshToken(
            string token
            )
        {
            JwtSettings jwtSettings = _config.GetSection("JwtSettings").Get<JwtSettings>();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
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

                // Ensure the token is a JWT token
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    var tokenType = jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenType")?.Value;

                    if (tokenType == null || tokenType != "refresh")
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    


}