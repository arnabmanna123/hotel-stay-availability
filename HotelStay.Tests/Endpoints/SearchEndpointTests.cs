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

        var payload = await response.Content.ReadFromJsonAsync<SearchResponsePayload>(ApiFixture.JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(3, payload!.Nights);
        Assert.Equal("USD", payload.Currency);
        Assert.NotEmpty(payload.Results);
        Assert.Equal(5, payload.Results.Count);
        Assert.Equal("BN-LON-STD-001", payload.Results[0].Id);
        Assert.Equal(75m, payload.Results[0].PricePerNight);
        Assert.Equal(225m, payload.Results[0].TotalPrice);
        Assert.Equal("BudgetNests", payload.Results[0].ProviderId);
        Assert.Equal("BN-LON-DLX-001", payload.Results[1].Id);
        Assert.Equal(110m, payload.Results[1].PricePerNight);
        Assert.Equal(330m, payload.Results[1].TotalPrice);
        Assert.Equal("PS-LON-STE-001", payload.Results[^1].Id);
        Assert.Equal(320m, payload.Results[^1].PricePerNight);
        Assert.Equal(960m, payload.Results[^1].TotalPrice);
        Assert.True(payload.Results[0].TotalPrice <= payload.Results[1].TotalPrice);
        Assert.True(payload.Results[1].TotalPrice <= payload.Results[^1].TotalPrice);
    }

    [Fact]
    public async Task Returns_400_when_destination_is_missing()
    {
        var response = await _client.GetAsync("/hotels/search?checkIn=2026-08-01&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.MissingDestination, error!.Code);
    }

    [Fact]
    public async Task Returns_400_when_checkIn_is_missing()
    {
        var response = await _client.GetAsync("/hotels/search?destination=London&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.MissingCheckIn, error!.Code);
    }

    [Fact]
    public async Task Returns_400_when_checkOut_is_missing()
    {
        var response = await _client.GetAsync("/hotels/search?destination=London&checkIn=2026-08-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.MissingCheckOut, error!.Code);
    }

    [Fact]
    public async Task Returns_400_when_dates_are_malformed()
    {
        var response = await _client.GetAsync("/hotels/search?destination=London&checkIn=not-a-date&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_400_when_checkOut_is_not_after_checkIn()
    {
        var response = await _client.GetAsync("/hotels/search?destination=London&checkIn=2026-08-04&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.InvalidDates, error!.Code);
    }

    [Fact]
    public async Task Returns_400_when_destination_is_unknown()
    {
        var response = await _client.GetAsync("/hotels/search?destination=Atlantis&checkIn=2026-08-01&checkOut=2026-08-04");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.UnknownDestination, error!.Code);
    }

    private sealed class SearchResponsePayload
    {
        public required List<RoomPayload> Results { get; init; }
        public int Nights { get; init; }
        public required string Currency { get; init; }
    }

    private sealed class RoomPayload
    {
        public required string Id { get; init; }
        public required string ProviderId { get; init; }
        public decimal PricePerNight { get; init; }
        public decimal TotalPrice { get; init; }
    }
}
