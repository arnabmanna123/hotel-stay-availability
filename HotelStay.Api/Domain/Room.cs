using System.Collections.Generic;

namespace HotelStay.Api.Domain;

public record Room(
    string Id,
    ProviderId ProviderId,
    RoomType RoomType,
    decimal PricePerNight,
    decimal TotalPrice,
    string Currency,
    CancellationPolicy CancellationPolicy,
    IReadOnlyList<string> Amenities,
    int? StarRating);