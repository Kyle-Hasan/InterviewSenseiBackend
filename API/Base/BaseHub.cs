using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace API.Base;

public abstract class BaseHub: Hub
{
    private string _entityType;

    public BaseHub(string entityType)
    {
        _entityType = entityType;
    }
    public async Task EntitiesUpdated(string messageType,string groupName, string message)
    {
        await Clients.Groups(groupName).SendAsync(messageType, message);
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException();
        }
            
        string idString = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        await Groups.AddToGroupAsync(Context.ConnectionId, _entityType);
        await base.OnConnectedAsync();
        
    }

    public async Task UnsubscribeFromGroup()
    {
        var httpContext = Context.GetHttpContext();
        
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException();
        }
            
        string idString = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, _entityType);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeFromGroup();
        await base.OnDisconnectedAsync(exception);
    }
}