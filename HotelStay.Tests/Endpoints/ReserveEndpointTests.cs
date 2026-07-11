using System.Net;
using System.Net.Http.Json;
using HotelStay.Api.Domain;

namespace HotelStay.Tests.Endpoints;

public class ReserveEndpointTests : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public ReserveEndpointTests(ApiFixture fixture) => _client = fixture.CreateClient();

    private static object HappyDomesticRequest() => new
    {
        roomId = "PS-LON-STD-001",
        providerId = "PremierStays",
        destination = "London",
        checkIn = "2026-08-01",
        checkOut = "2026-08-04",
        guestName = "Ada Lovelace",
        documentType = "NationalId",
        documentNumber = "AL-1815-XYZ",
    };

    [Fact]
    public async Task Returns_200_and_reservation_for_valid_domestic_request()
    {
        var response = await _client.PostAsJsonAsync("/hotels/reserve", HappyDomesticRequest());
        response.EnsureSuccessStatusCode();

        var reservation = await response.Content.ReadFromJsonAsync<Reservation>(ApiFixture.JsonOptions);
        Assert.NotNull(reservation);
        Assert.StartsWith("HS-", reservation!.Reference);
        Assert.Equal(ProviderId.PremierStays, reservation.ProviderId);
        Assert.Equal(3, reservation.Nights);
        Assert.Equal(360m, reservation.TotalPrice);
        Assert.Equal(DocumentType.NationalId, reservation.DocumentType);
    }

    [Fact]
    public async Task Returns_422_when_international_destination_receives_NationalId()
    {
        var request = new
        {
            roomId = "PS-NYC-STD-001",
            providerId = "PremierStays",
            destination = "New York",
            checkIn = "2026-08-01",
            checkOut = "2026-08-04",
            guestName = "Ada Lovelace",
            documentType = "NationalId",
            documentNumber = "AL-1815-XYZ",
        };

        var response = await _client.PostAsJsonAsync("/hotels/reserve", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.DocumentRequiredPassport, error!.Code);
    }

    [Fact]
    public async Task Returns_200_when_international_destination_receives_Passport()
    {
        var request = new
        {
            roomId = "PS-NYC-STD-001",
            providerId = "PremierStays",
            destination = "New York",
            checkIn = "2026-08-01",
            checkOut = "2026-08-04",
            guestName = "Ada Lovelace",
            documentType = "Passport",
            documentNumber = "PP-123456",
        };

        var response = await _client.PostAsJsonAsync("/hotels/reserve", request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Returns_400_when_room_is_not_found()
    {
        var request = new
        {
            roomId = "PS-DOES-NOT-EXIST",
            providerId = "PremierStays",
            destination = "London",
            checkIn = "2026-08-01",
            checkOut = "2026-08-04",
            guestName = "Ada Lovelace",
            documentType = "NationalId",
            documentNumber = "AL-1815-XYZ",
        };

        var response = await _client.PostAsJsonAsync("/hotels/reserve", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.RoomNotFound, error!.Code);
    }

    [Fact]
    public async Task Returns_400_when_guestName_is_missing()
    {
        var request = new
        {
            roomId = "PS-LON-STD-001",
            providerId = "PremierStays",
            destination = "London",
            checkIn = "2026-08-01",
            checkOut = "2026-08-04",
            guestName = "",
            documentType = "NationalId",
            documentNumber = "AL-1815-XYZ",
        };

        var response = await _client.PostAsJsonAsync("/hotels/reserve", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiFixture.JsonOptions);
        Assert.Equal(ErrorCodes.GuestNameRequired, error!.Code);
    }
}
