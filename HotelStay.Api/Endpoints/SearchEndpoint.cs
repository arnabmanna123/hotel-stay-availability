using HotelStay.Api.Domain;
using HotelStay.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelStay.Api.Endpoints;

public static class SearchEndpoint
{
    public static void MapSearchEndpoint(this WebApplication app)
    {
        app.MapGet("/hotels/search", async (string? destination, DateTime? checkIn, DateTime? checkOut, RoomType? roomType, [FromServices] ICityCatalogue catalog, [FromServices] HotelAggregator aggregator) =>
        {
            if (string.IsNullOrWhiteSpace(destination) || !checkIn.HasValue || !checkOut.HasValue)
            {
                return Results.BadRequest(new ApiError("Missing required query parameters.", ErrorCodes.ValidationFailure));
            }

            if (checkIn >= checkOut)
            {
                return Results.BadRequest(new ApiError("Check-out must be after check-in.", ErrorCodes.ValidationFailure));
            }

            if (!catalog.TryGetCityClass(destination, out _))
            {
                return Results.NotFound(new ApiError("Destination not found.", ErrorCodes.NotFound));
            }

            var query = new SearchQuery(destination.Trim(), checkIn.Value, checkOut.Value, roomType);
            var rooms = await aggregator.SearchAsync(query, CancellationToken.None);
            var nights = (checkOut.Value - checkIn.Value).Days;
            var currency = rooms.Count > 0 ? rooms[0].Currency : "USD";

            return Results.Ok(new { results = rooms, nights, currency });
        })
        .WithName("SearchHotels");
    }
}
