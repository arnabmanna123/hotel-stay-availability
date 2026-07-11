using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HotelStay.Tests.Endpoints;

/// <summary>
/// Shared WebApplicationFactory used across all endpoint tests.
/// Provides a preconfigured JsonSerializerOptions matching the API's response contract
/// (camelCase properties, enums as strings) so tests deserialize responses correctly.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReservationsFilePath"] = Path.Combine(
                    Path.GetTempPath(),
                    $"hotelstay-reservations-{Guid.NewGuid():N}.json"),
            });
        });

        base.ConfigureWebHost(builder);
    }
}
