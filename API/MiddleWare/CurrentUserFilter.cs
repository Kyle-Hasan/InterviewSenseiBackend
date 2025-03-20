using API.Base;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.MiddleWare;

public class CurrentUserFilter : IAsyncActionFilter
{
  
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasIgnoreFilter = context.Controller.GetType().GetCustomAttributes(typeof(IgnoreUserFetchFilterAttribute), true).Any();

        if (hasIgnoreFilter)
        {
            await next();
            return;
        }
        if (context.Controller is BaseController controller)
        {
            controller.CurrentUser = await controller.GetCurrentUser1();
        }

        await next();
    }
}