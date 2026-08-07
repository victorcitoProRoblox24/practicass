using Microsoft.Data.Sqlite;

namespace MiAppNetCore.Services;

public class CityWeather
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double TemperatureC { get; set; }
}

public class WeatherService
{
    private readonly string _connectionString;

    public WeatherService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WeatherDb")
            ?? "Data Source=weather.db";
        EnsureDatabase();
    }

    private void EnsureDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createTable = connection.CreateCommand();
        createTable.CommandText =
            "CREATE TABLE IF NOT EXISTS Cities (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, TemperatureC REAL NOT NULL)";
        createTable.ExecuteNonQuery();

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Cities";
        var count = (long)countCommand.ExecuteScalar()!;
        if (count > 0)
        {
            return;
        }

        var seedData = new (string Name, double TemperatureC)[]
        {
            ("Ciudad de Mexico", 22.5),
            ("Guadalajara", 26.0),
            ("Monterrey", 31.2),
            ("Tijuana", 19.8)
        };

        foreach (var city in seedData)
        {
            var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO Cities (Name, TemperatureC) VALUES ($name, $temp)";
            insert.Parameters.AddWithValue("$name", city.Name);
            insert.Parameters.AddWithValue("$temp", city.TemperatureC);
            insert.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Busca el clima de una ciudad por nombre.
    /// VULNERABLE A PROPOSITO: concatena el input del usuario directamente en la
    /// consulta SQL en lugar de usar parametros. CORREGIDO: se usa un parametro
    /// ($namePattern) para que el motor de SQLite trate el input del usuario
    /// siempre como dato, nunca como parte del comando SQL.
    /// </summary>
    public List<CityWeather> SearchCityWeather(string cityName)
    {
        var results = new List<CityWeather>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Name, TemperatureC FROM Cities WHERE Name LIKE $namePattern";
        command.Parameters.AddWithValue("$namePattern", $"%{cityName}%");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new CityWeather
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                TemperatureC = reader.GetDouble(2)
            });
        }

        return results;
    }

    /// <summary>
    /// Valida si una temperatura esta dentro de un rango fisicamente posible.
    /// CORREGIDO: se usa AND para que ambos limites del rango se cumplan a la vez,
    /// en lugar de OR (que hacia que la condicion fuera practicamente siempre verdadera).
    /// </summary>
    public static bool IsValidTemperature(double temperatureC)
    {
        return temperatureC >= -273.15 && temperatureC <= 1000;
    }

    /// <summary>
    /// Devuelve una descripcion textual de la temperatura.
    /// CORREGIDO: se elimino la variable local 'unitLabel' que nunca se usaba.
    /// </summary>
    public static string GetWeatherDescription(double temperatureC)
    {
        if (temperatureC < 0)
        {
            return "Congelando";
        }
        if (temperatureC < 15)
        {
            return "Frio";
        }
        if (temperatureC < 25)
        {
            return "Templado";
        }
        if (temperatureC < 35)
        {
            return "Calido";
        }

        return "Muy caluroso";
    }

    /// <summary>
    /// Devuelve una descripcion legible del clima de una ciudad.
    /// CORREGIDO: se valida que la busqueda haya encontrado resultados antes de
    /// acceder a la propiedad, evitando la NullReferenceException.
    /// </summary>
    public string GetCityWeatherSummary(string cityName)
    {
        CityWeather? match = SearchCityWeather(cityName).FirstOrDefault();
        if (match is null)
        {
            return $"No se encontraron resultados para '{cityName}'.";
        }

        return $"{match.Name}: {GetWeatherDescription(match.TemperatureC)}";
    }
}
