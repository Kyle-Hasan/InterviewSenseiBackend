using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API
{
    public class JwtSettings
    {
        public required string SecretKey {get;set;}
        public string? Issuer {get;set;} = null;

        public string? Audience {get;set;} = null;

        public int AccessTokenExpirationMinutes {get;set;}

        public int RefreshTokenExpirationDays {get;set;}
    }
}