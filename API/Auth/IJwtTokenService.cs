using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Users;

namespace API.Auth
{
    public interface IJwtTokenService
    {
        Task<string> GenerateToken(AppUser appUser,bool refreshToken);
        
        // return -1 if invalid token , return user id if valid token

        int ValidateRefreshToken(string token);
    }
}