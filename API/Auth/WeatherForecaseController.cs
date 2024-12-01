using API.AI;
using API.Base;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]

public class WeatherForecastController(IOpenAIService openAiService,UserManager<AppUser> userManager) : BaseController(userManager)
{
    [HttpGet]
    public async Task<string> getWeather()
    {
        return null;
    }
}