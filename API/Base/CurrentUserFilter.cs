using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Base;

public class CurrentUserFilter : IAsyncActionFilter
{
  
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is BaseController controller)
        {
            controller.CurrentUser = await controller.GetCurrentUser1();
        }

        await next();
    }
}