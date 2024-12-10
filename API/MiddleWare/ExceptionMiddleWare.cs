using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using API.Base;


namespace API.MiddleWare
{
    public class ExceptionMiddleWare(RequestDelegate next, ILogger<ExceptionMiddleWare> logger, IHostEnvironment env )
    {
        public async Task InvokeAsync(HttpContext context) {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                
                logger.LogError(ex,ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var response = env.IsDevelopment() ? new ApiException(context.Response.StatusCode,ex.Message,ex.StackTrace) 
                    :  new ApiException(context.Response.StatusCode,ex.Message,"Internal server error");
                var options = new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(response,options);
                await context.Response.WriteAsync(json);
            }
        }
    }
}