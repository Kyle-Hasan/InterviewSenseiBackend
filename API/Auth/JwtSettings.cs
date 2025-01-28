using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    public static class JwtSettings
    {
        public static string SecretKey {get;set;} = Environment.GetEnvironmentVariable("JWT_SecretKey");
        public static string? Issuer {get;set;} = Environment.GetEnvironmentVariable("JWT_Issuer");

        public static string? Audience {get;set;} = Environment.GetEnvironmentVariable("JWT_Audience");

        public static string AccessTokenExpirationMinutes {get;set;} = Environment.GetEnvironmentVariable("JWT_AccessTokenExpirationMinutes");

        public static string RefreshTokenExpirationDays {get;set;} = Environment.GetEnvironmentVariable("JWT_RefreshTokenExpirationDays");

        
    }
}