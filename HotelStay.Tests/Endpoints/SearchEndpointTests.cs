using System.Net;
using System.Net.Http.Json;
using HotelStay.Api.Domain;

namespace HotelStay.Tests.Endpoints;

public class SearchEndpointTests : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public SearchEndpointTests(ApiFixture fixture) => _client = fixture.CreateClient();

    [Fact]
    public async Task Returns_200_with_results_for_known_destination()
    {
        var response = await _client.GetAsync("/hotels/search?destination=London&checkIn=2026-08-01&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("results", content);
        Assert.Contains("currency", content);
    }

    [Fact]
    public async Task Returns_400_when_destination_is_unknown()
    {
        var response = await _client.GetAsync("/hotels/search?destination=Atlantis&checkIn=2026-08-01&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.UnknownDestination, error!.Code);
    }
}
