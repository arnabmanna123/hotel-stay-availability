namespace HotelStay.Api.Providers.BudgetNests;

internal sealed class BudgetNestsRoomDto
{
    public required string Id { get; init; }
    public required string Destination { get; init; }
    public required string RoomType { get; init; }     // JSON: room_type
    public required decimal PricePerNight { get; init; } // JSON: price_per_night
    public required string Cancellation { get; init; }
    public bool Available { get; init; } = true;
}
