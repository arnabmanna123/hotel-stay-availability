using HotelStay.Api.Domain;
using HotelStay.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelStay.Api.Endpoints;

public static class ReservationEndpoint
{
    public record ReserveRequest(
        string? RoomId,
        ProviderId? ProviderId,
        string? Destination,
        DateOnly? CheckIn,
        DateOnly? CheckOut,
        string? GuestName,
        DocumentType? DocumentType,
        string? DocumentNumber);

    public static IEndpointRouteBuilder MapReservationEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/hotels/reserve", ReserveHotelAsync);

        app.MapGet("/hotels/reservation/{reference}", GetReservation)
            .WithName("GetReservation");

        return app;
    }

    private static async Task<IResult> ReserveHotelAsync(
        [FromBody] ReserveRequest? body,
        [FromServices] HotelAggregator aggregator,
        [FromServices] ICityCatalogue cities,
        [FromServices] IDocumentValidator documentValidator,
        [FromServices] IReferenceNumberFactory referenceFactory,
        [FromServices] IReservationStore store,
        CancellationToken ct)
    {
        if (body is null)
            return Results.BadRequest(new ApiError("Request body is required", ErrorCodes.InvalidBody));

        if (string.IsNullOrWhiteSpace(body.RoomId))
            return Results.BadRequest(new ApiError("roomId is required", ErrorCodes.RoomNotFound));

        if (body.ProviderId is null)
            return Results.BadRequest(new ApiError("providerId is required", ErrorCodes.UnknownProvider));

        if (string.IsNullOrWhiteSpace(body.Destination))
            return Results.BadRequest(new ApiError("destination is required", ErrorCodes.MissingDestination));

        if (body.CheckIn is null)
            return Results.BadRequest(new ApiError("checkIn is required", ErrorCodes.MissingCheckIn));

        if (body.CheckOut is null)
            return Results.BadRequest(new ApiError("checkOut is required", ErrorCodes.MissingCheckOut));

        if (body.CheckOut <= body.CheckIn)
            return Results.BadRequest(new ApiError(
                "checkOut must be after checkIn",
                ErrorCodes.InvalidDates));

        if (string.IsNullOrWhiteSpace(body.GuestName))
            return Results.BadRequest(new ApiError("guestName is required", ErrorCodes.GuestNameRequired));

        if (body.DocumentType is null)
            return Results.BadRequest(new ApiError(
                "documentType is required (Passport or NationalId)",
                ErrorCodes.DocumentTypeRequired));

        var city = cities.Find(body.Destination);
        if (city is null)
            return Results.BadRequest(new ApiError(
                $"Unknown destination: {body.Destination}",
                ErrorCodes.UnknownDestination));

        var docError = documentValidator.Validate(city.Class, body.DocumentType.Value, body.DocumentNumber);
        if (docError is not null)
            return Results.UnprocessableEntity(docError);

        var query = new SearchQuery(
            city.Name,
            body.CheckIn.Value.ToDateTime(TimeOnly.MinValue),
            body.CheckOut.Value.ToDateTime(TimeOnly.MinValue),
            null);
        var room = await aggregator.FindRoomAsync(body.ProviderId.Value, body.RoomId, query, ct);
        if (room is null)
            return Results.BadRequest(new ApiError(
                "Room not found for the given provider, destination, and dates",
                ErrorCodes.RoomNotFound));

        var nights = body.CheckOut.Value.DayNumber - body.CheckIn.Value.DayNumber;
        var reservation = new Reservation(
            Reference: referenceFactory.Generate(),
            ProviderId: room.ProviderId,
            RoomId: room.Id,
            RoomType: room.RoomType,
            CheckIn: body.CheckIn.Value,
            CheckOut: body.CheckOut.Value,
            Nights: nights,
            PricePerNight: room.PricePerNight,
            TotalPrice: room.TotalPrice,
            Currency: room.Currency,
            CancellationPolicy: room.CancellationPolicy,
            GuestName: body.GuestName,
            DocumentType: body.DocumentType.Value,
            DocumentNumber: body.DocumentNumber!);

        if (!store.TrySave(reservation))
        {
            return Results.Conflict(new ApiError(
                "A reservation with the same reference already exists.",
                ErrorCodes.ValidationFailure));
        }

        return TypedResults.Ok(reservation);
    }

    private static IResult GetReservation(string reference, IReservationStore store)
    {
        var reservation = store.Find(reference);
        return reservation is null
            ? Results.NotFound(new ApiError("Reservation not found", ErrorCodes.ReservationNotFound))
            : TypedResults.Ok(reservation);
    }
}
