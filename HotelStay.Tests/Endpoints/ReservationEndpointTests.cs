using System.Net;
using System.Net.Http.Json;
using HotelStay.Api.Domain;

namespace HotelStay.Tests.Endpoints;

public class ReservationEndpointTests : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public ReservationEndpointTests(ApiFixture fixture) => _client = fixture.CreateClient();

    [Fact]
    public async Task Reserve_then_GET_returns_the_same_reservation()
    {
        var reserveResponse = await _client.PostAsJsonAsync("/hotels/reserve", new
        {
            roomId = "PS-LON-STD-001",
            providerId = "PremierStays",
            destination = "London",
            checkIn = "2026-09-01",
            checkOut = "2026-09-03",
            guestName = "Grace Hopper",
            documentType = "NationalId",
            documentNumber = "GH-1906",
        });
        reserveResponse.EnsureSuccessStatusCode();

        var created = await reserveResponse.Content.ReadFromJsonAsync<Reservation>(ApiFixture.JsonOptions);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/hotels/reservation/{created!.Reference}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<Reservation>(ApiFixture.JsonOptions);
        Assert.Equal(created.Reference, fetched!.Reference);
        Assert.Equal(created.TotalPrice, fetched.TotalPrice);
        Assert.Equal(created.GuestName, fetched.GuestName);
    }

    [Fact]
    public async Task Returns_404_for_unknown_reference()
    {
        var response = await _client.GetAsync("/hotels/reservation/HS-NOTFOUND");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.ReservationNotFound, error!.Code);
    }
}
