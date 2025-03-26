namespace API.MiddleWare;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;

public class CsrfValidationFilter(IAntiforgery antiforgery) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Check if the action or its controller has the IgnoreCsrfValidation attribute.
        var hasIgnoreAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<IgnoreCsrfValidationAttribute>()
            .Any();

        if (hasIgnoreAttribute)
        {
            await next();
            return;
        }
        // only on methods that can change stuff
        if (HttpMethods.IsPost(context.HttpContext.Request.Method) ||
            HttpMethods.IsPut(context.HttpContext.Request.Method) ||
            HttpMethods.IsDelete(context.HttpContext.Request.Method) ||
            HttpMethods.IsPatch(context.HttpContext.Request.Method))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new BadRequestObjectResult("CSRF token validation failed.");
                return;
            }
        }

        await next();
    }
}
