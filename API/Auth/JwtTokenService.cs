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
    

    public JwtTokenService() {
        
    }

    public async Task<string> GenerateToken(AppUser user, bool refreshToken)
    {

        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new(ClaimTypes.Name,user.UserName),
            new("tokenType", refreshToken ? "refresh" : "access")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey));
        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha512);

        DateTime expiration = refreshToken ? DateTime.Now.AddDays(int.Parse(JwtSettings.RefreshTokenExpirationDays)) : 
        DateTime.Now.AddMinutes(int.Parse(JwtSettings.AccessTokenExpirationMinutes));

        var token = new JwtSecurityToken(
            
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    // return -1 if invalid token , return user id if valid token
        public  int ValidateRefreshToken(
            string token
            )
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
                    int? userId = int.Parse((jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value));
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
            catch (Exception ex)
            {
                return -1;
            }

            return -1;
        }
    


}