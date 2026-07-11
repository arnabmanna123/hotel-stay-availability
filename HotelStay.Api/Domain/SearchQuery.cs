namespace HotelStay.Api.Domain;

public sealed record SearchQuery(string Destination, DateTime CheckIn, DateTime CheckOut, RoomType? RoomType);
