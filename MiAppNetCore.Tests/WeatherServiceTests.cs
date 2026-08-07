using Microsoft.Extensions.Configuration;
using MiAppNetCore.Services;
using Xunit;

namespace MiAppNetCore.Tests;

public class WeatherServiceTests
{
    private static WeatherService CreateService(string dbName)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"{dbName}_{Guid.NewGuid():N}.db");

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:WeatherDb"] = $"Data Source={dbPath}"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new WeatherService(configuration);
    }

    [Theory]
    [InlineData(-273.15)]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(1000)]
    public void IsValidTemperature_ReturnsTrue_WithinPhysicalRange(double temperature)
    {
        Assert.True(WeatherService.IsValidTemperature(temperature));
    }

    [Theory]
    [InlineData(-300)]
    [InlineData(5000)]
    public void IsValidTemperature_ReturnsFalse_OutsidePhysicalRange(double temperature)
    {
        Assert.False(WeatherService.IsValidTemperature(temperature));
    }

    [Theory]
    [InlineData(-10, "Congelando")]
    [InlineData(5, "Frio")]
    [InlineData(20, "Templado")]
    [InlineData(30, "Calido")]
    [InlineData(40, "Muy caluroso")]
    public void GetWeatherDescription_ReturnsExpectedLabel(double temperature, string expected)
    {
        var result = WeatherService.GetWeatherDescription(temperature);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SearchCityWeather_ReturnsSeededCities_WhenNameMatches()
    {
        var service = CreateService(nameof(SearchCityWeather_ReturnsSeededCities_WhenNameMatches));

        var results = service.SearchCityWeather("Guadalajara");

        Assert.Single(results);
        Assert.Equal("Guadalajara", results[0].Name);
    }

    [Fact]
    public void SearchCityWeather_ReturnsEmpty_WhenNoMatch()
    {
        var service = CreateService(nameof(SearchCityWeather_ReturnsEmpty_WhenNoMatch));

        var results = service.SearchCityWeather("CiudadInexistente");

        Assert.Empty(results);
    }

    [Fact]
    public void GetCityWeatherSummary_ReturnsMessage_WhenCityNotFound()
    {
        var service = CreateService(nameof(GetCityWeatherSummary_ReturnsMessage_WhenCityNotFound));

        var summary = service.GetCityWeatherSummary("CiudadInexistente");

        Assert.Contains("No se encontraron resultados", summary);
    }

    [Fact]
    public void GetCityWeatherSummary_ReturnsDescription_WhenCityFound()
    {
        var service = CreateService(nameof(GetCityWeatherSummary_ReturnsDescription_WhenCityFound));

        var summary = service.GetCityWeatherSummary("Monterrey");

        Assert.StartsWith("Monterrey:", summary);
    }
}
