namespace HotelStay.Api.Domain;

public record Reservation(
    string Reference,
    ProviderId ProviderId,
    string RoomId,
    RoomType RoomType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal PricePerNight,
    decimal TotalPrice,
    string Currency,
    CancellationPolicy CancellationPolicy,
    string GuestName,
    DocumentType DocumentType,
    string DocumentNumber);
