using HotelStay.Api.Domain;
using HotelStay.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelStay.Api.Endpoints;

public static class SearchEndpoint
{
    public static void MapSearchEndpoint(this WebApplication app)
    {
        app.MapGet("/hotels/search", async (
            string? destination,
            DateTime? checkIn,
            DateTime? checkOut,
            RoomType? roomType,
            [FromServices] ICityCatalogue catalog,
            [FromServices] HotelAggregator aggregator,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                return Results.BadRequest(new ApiError("destination is required", ErrorCodes.MissingDestination));
            }

            if (!checkIn.HasValue)
            {
                return Results.BadRequest(new ApiError("checkIn is required", ErrorCodes.MissingCheckIn));
            }

            if (!checkOut.HasValue)
            {
                return Results.BadRequest(new ApiError("checkOut is required", ErrorCodes.MissingCheckOut));
            }

            if (checkIn >= checkOut)
            {
                return Results.BadRequest(new ApiError("checkOut must be after checkIn", ErrorCodes.InvalidDates));
            }

            if (!catalog.TryGetCityClass(destination, out _))
            {
                return Results.BadRequest(new ApiError($"Unknown destination: {destination}", ErrorCodes.UnknownDestination));
            }

            var query = new SearchQuery(destination.Trim(), checkIn.Value, checkOut.Value, roomType);
            var rooms = await aggregator.SearchAsync(query, cancellationToken);
            var nights = (checkOut.Value - checkIn.Value).Days;
            var currency = rooms.Count > 0 ? rooms[0].Currency : "USD";

            return TypedResults.Ok(new { results = rooms, nights, currency });
        })
        .WithName("SearchHotels");
    }
}
