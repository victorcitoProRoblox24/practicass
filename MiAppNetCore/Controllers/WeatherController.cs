using Microsoft.AspNetCore.Mvc;
using MiAppNetCore.Services;

namespace MiAppNetCore.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly WeatherService _weatherService;

    public WeatherController(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("search")]
    public ActionResult<List<CityWeather>> Search([FromQuery] string city)
    {
        var results = _weatherService.SearchCityWeather(city);
        return Ok(results);
    }

    [HttpGet("valid")]
    public ActionResult<bool> IsValid([FromQuery] double temperatureC)
    {
        return Ok(WeatherService.IsValidTemperature(temperatureC));
    }

    [HttpGet("description")]
    public ActionResult<string> Description([FromQuery] double temperatureC)
    {
        return Ok(WeatherService.GetWeatherDescription(temperatureC));
    }

    [HttpGet("summary")]
    public ActionResult<string> Summary([FromQuery] string city)
    {
        return Ok(_weatherService.GetCityWeatherSummary(city));
    }
}
