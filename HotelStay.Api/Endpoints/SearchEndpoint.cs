using System.Globalization;
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
            string? checkIn,
            string? checkOut,
            RoomType? roomType,
            [FromServices] ICityCatalogue catalog,
            [FromServices] HotelAggregator aggregator,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                return Results.BadRequest(new ApiError("destination is required", ErrorCodes.MissingDestination));
            }

            if (string.IsNullOrWhiteSpace(checkIn))
            {
                return Results.BadRequest(new ApiError("checkIn is required", ErrorCodes.MissingCheckIn));
            }

            if (string.IsNullOrWhiteSpace(checkOut))
            {
                return Results.BadRequest(new ApiError("checkOut is required", ErrorCodes.MissingCheckOut));
            }

            var parsedCheckIn = DateTime.TryParse(checkIn, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedCheckInDate);
            var parsedCheckOut = DateTime.TryParse(checkOut, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedCheckOutDate);

            if (!parsedCheckIn || !parsedCheckOut)
            {
                return Results.BadRequest(new ApiError("checkIn and checkOut must be valid dates", ErrorCodes.InvalidDates));
            }

            if (parsedCheckInDate >= parsedCheckOutDate)
            {
                return Results.BadRequest(new ApiError("checkOut must be after checkIn", ErrorCodes.InvalidDates));
            }

            if (!catalog.TryGetCityClass(destination, out _))
            {
                return Results.BadRequest(new ApiError($"Unknown destination: {destination}", ErrorCodes.UnknownDestination));
            }

            var query = new SearchQuery(destination.Trim(), parsedCheckInDate, parsedCheckOutDate, roomType);
            var rooms = await aggregator.SearchAsync(query, cancellationToken);
            var nights = (parsedCheckOutDate - parsedCheckInDate).Days;
            var currency = rooms.Count > 0 ? rooms[0].Currency : "USD";

            return TypedResults.Ok(new { results = rooms, nights, currency });
        })
        .WithName("SearchHotels");
    }
}
