using System.Text.Json;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers.PremierStays;

public sealed class PremierStaysProvider : IHotelProvider
{
    private readonly IReadOnlyList<PremierStaysRoomDto> _rooms;
    private readonly ILogger<PremierStaysProvider> _logger;

    public ProviderId Id => ProviderId.PremierStays;

    public PremierStaysProvider(ILogger<PremierStaysProvider> logger)
    {
        _logger = logger;

        // PascalCase JSON: PropertyNamingPolicy = null matches the properties as-is.
        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };

        var assembly = typeof(PremierStaysProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream("premierstays.rooms.json")
            ?? throw new InvalidOperationException("premierstays.rooms.json not found in embedded resources");

        _rooms = JsonSerializer.Deserialize<List<PremierStaysRoomDto>>(stream, options)
            ?? throw new InvalidOperationException("premierstays.rooms.json deserialised to null");
    }

    public Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct)
    {
        var nights = (query.CheckOut - query.CheckIn).Days;

        var matches = _rooms
            .Where(r => string.Equals(r.Destination, query.Destination, StringComparison.OrdinalIgnoreCase))
            .Where(r => query.RoomType is null
                     || string.Equals(r.RoomType, query.RoomType.Value.ToString(), StringComparison.OrdinalIgnoreCase))
            .Select(r => Normalise(r, nights))
            .ToList();

        _logger.LogInformation("PremierStays: {Count} rooms for destination={Destination}",
            matches.Count, query.Destination);

        return Task.FromResult<IReadOnlyList<Room>>(matches);
    }

    internal static Room Normalise(PremierStaysRoomDto dto, int nights)
    {
        var roomType = Enum.Parse<RoomType>(dto.RoomType, ignoreCase: true);

        var policy = dto.Cancellation.ToLowerInvariant() switch
        {
            "freecancellation" => new CancellationPolicy(CancellationPolicyType.FreeCancellation, 48),
            "nonrefundable" => new CancellationPolicy(CancellationPolicyType.NonRefundable, null),
            _ => throw new InvalidOperationException(
                $"Unknown PremierStays cancellation value: {dto.Cancellation}")
        };

        return new Room(
            Id: dto.Id,
            ProviderId: ProviderId.PremierStays,
            RoomType: roomType,
            PricePerNight: dto.PricePerNight,
            TotalPrice: dto.PricePerNight * nights,
            Currency: "USD",
            CancellationPolicy: policy,
            Amenities: dto.Amenities,
            StarRating: dto.StarRating);
    }
}
