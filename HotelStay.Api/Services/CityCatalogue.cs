using System.Text.Json;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Services;

public record CityInfo(string Name, CityClass Class);

public interface ICityCatalogue
{
    IReadOnlyList<CityInfo> All { get; }
    CityInfo? Find(string name);
    Task<IReadOnlyList<CityInfo>> GetCitiesAsync();
    bool TryGetCityClass(string name, out CityClass? cityClass);
}

public sealed class CityCatalogue : ICityCatalogue
{
    public IReadOnlyList<CityInfo> All { get; }

    public CityCatalogue()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var assembly = typeof(CityCatalogue).Assembly;
        using var stream = assembly.GetManifestResourceStream("cities.json")
            ?? throw new InvalidOperationException("cities.json not found in embedded resources");

        var dtos = JsonSerializer.Deserialize<List<CityDto>>(stream, options)
            ?? throw new InvalidOperationException("cities.json deserialised to null");

        All = dtos
            .Select(d => new CityInfo(d.Name, Enum.Parse<CityClass>(d.Class, ignoreCase: true)))
            .ToList();
    }

    public CityInfo? Find(string name) =>
        All.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    public Task<IReadOnlyList<CityInfo>> GetCitiesAsync() => Task.FromResult(All);

    public bool TryGetCityClass(string name, out CityClass? cityClass)
    {
        var city = Find(name);
        if (city is null)
        {
            cityClass = null;
            return false;
        }

        cityClass = city.Class;
        return true;
    }

    private sealed class CityDto
    {
        public required string Name { get; init; }
        public required string Class { get; init; }
    }
}
