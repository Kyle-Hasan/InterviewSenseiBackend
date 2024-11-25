using API.AI;
using API.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]

public class WeatherForecastController(IOpenAIService openAiService) : BaseController
{
    [HttpGet]
    public async Task<string> getWeather()
    {
        return null;
    }
}