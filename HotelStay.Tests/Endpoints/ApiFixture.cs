using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

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
}
