namespace HotelStay.Api.Providers.PremierStays;

internal sealed class PremierStaysRoomDto
{
    public required string Id { get; init; }
    public required string Destination { get; init; }
    public required string RoomType { get; init; }
    public required decimal PricePerNight { get; init; }
    public required string Cancellation { get; init; }
    public List<string> Amenities { get; init; } = new();
    public int? StarRating { get; init; }
}
