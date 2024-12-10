using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Base
{
    public class ApiException(int StatusCode, string message, string? details )
    {
        public int StatusCode {get;set;} = StatusCode;
        public string Message {get;set;} = message;

        public string? Details {get;set;} = details;

    }
}