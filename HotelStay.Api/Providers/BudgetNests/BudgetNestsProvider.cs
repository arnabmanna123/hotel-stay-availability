

using System.Text.Json;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Providers.BudgetNests;

public sealed class BudgetNestsProvider : IHotelProvider
{
    private readonly IReadOnlyList<BudgetNestsRoomDto> _rooms;
    private readonly ILogger<BudgetNestsProvider> _logger;

    public ProviderId Id => ProviderId.BudgetNests;

    public BudgetNestsProvider(ILogger<BudgetNestsProvider> logger)
    {
        _logger = logger;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var assembly = typeof(BudgetNestsProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream("budgetnests.rooms.json")
            ?? throw new InvalidOperationException("budgetnests.rooms.json not found in embedded resources");

        _rooms = JsonSerializer.Deserialize<List<BudgetNestsRoomDto>>(stream, options)
            ?? throw new InvalidOperationException("budgetnests.rooms.json deserialised to null");
    }

    public Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct)
    {
        var nights = (query.CheckOut - query.CheckIn).Days;

        var matches = _rooms
            .Where(r => r.Available)
            .Where(r => string.Equals(r.Destination, query.Destination, StringComparison.OrdinalIgnoreCase))
            .Where(r => query.RoomType is null
                     || string.Equals(r.RoomType, query.RoomType.Value.ToString(), StringComparison.OrdinalIgnoreCase))
            .Select(r => Normalise(r, nights))
            .ToList();

        _logger.LogInformation("BudgetNests: {Count} rooms for destination={Destination}",
            matches.Count, query.Destination);

        return Task.FromResult<IReadOnlyList<Room>>(matches);
    }

    internal static Room Normalise(BudgetNestsRoomDto dto, int nights)
    {
        var roomType = Enum.Parse<RoomType>(dto.RoomType, ignoreCase: true);

        var policy = dto.Cancellation.ToLowerInvariant() switch
        {
            "flexible" => new CancellationPolicy(CancellationPolicyType.Flexible, 24),
            "nonrefundable" => new CancellationPolicy(CancellationPolicyType.NonRefundable, null),
            _ => throw new InvalidOperationException(
                $"Unknown BudgetNests cancellation value: {dto.Cancellation}")
        };

        return new Room(
            Id: dto.Id,
            ProviderId: ProviderId.BudgetNests,
            RoomType: roomType,
            PricePerNight: dto.PricePerNight,
            TotalPrice: dto.PricePerNight * nights,
            Currency: "USD",
            CancellationPolicy: policy,
            Amenities: Array.Empty<string>(),
            StarRating: null);
    }
}


