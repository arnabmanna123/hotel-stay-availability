using HotelStay.Api.Domain;
using HotelStay.Api.Providers;
using HotelStay.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelStay.Tests.Services;

public class HotelAggregatorTests
{
    private static readonly DateOnly CheckIn = new(2026, 8, 1);
    private static readonly DateOnly CheckOut = new(2026, 8, 4);

    [Fact]
    public async Task Merges_rooms_from_all_providers()
    {
        var provider1 = new StubProvider(ProviderId.PremierStays,
            [MakeRoom(ProviderId.PremierStays, "PS-1", 300m)]);
        var provider2 = new StubProvider(ProviderId.BudgetNests,
            [MakeRoom(ProviderId.BudgetNests, "BN-1", 200m)]);

        var aggregator = new HotelAggregator([provider1, provider2], NullLogger<HotelAggregator>.Instance);
        var rooms = await aggregator.SearchAsync(new SearchQuery("Anywhere", CheckIn.ToDateTime(TimeOnly.MinValue), CheckOut.ToDateTime(TimeOnly.MinValue), null), default);

        Assert.Equal(2, rooms.Count);
    }

    [Fact]
    public async Task Sorts_by_total_price_ascending_then_provider_id()
    {
        var provider1 = new StubProvider(ProviderId.PremierStays,
        [
            MakeRoom(ProviderId.PremierStays, "PS-EXPENSIVE", 500m),
            MakeRoom(ProviderId.PremierStays, "PS-CHEAP", 100m),
        ]);
        var provider2 = new StubProvider(ProviderId.BudgetNests,
        [
            MakeRoom(ProviderId.BudgetNests, "BN-MID", 300m),
        ]);

        var aggregator = new HotelAggregator([provider1, provider2], NullLogger<HotelAggregator>.Instance);
        var rooms = await aggregator.SearchAsync(new SearchQuery("Anywhere", CheckIn.ToDateTime(TimeOnly.MinValue), CheckOut.ToDateTime(TimeOnly.MinValue), null), default);

        Assert.Equal(new[] { "PS-CHEAP", "BN-MID", "PS-EXPENSIVE" }, rooms.Select(r => r.Id));
    }

    [Fact]
    public async Task Single_provider_failure_does_not_fail_the_whole_search()
    {
        var throwing = new ThrowingProvider(ProviderId.BudgetNests);
        var ok = new StubProvider(ProviderId.PremierStays,
            [MakeRoom(ProviderId.PremierStays, "PS-1", 200m)]);

        var aggregator = new HotelAggregator([throwing, ok], NullLogger<HotelAggregator>.Instance);
        var rooms = await aggregator.SearchAsync(new SearchQuery("Anywhere", CheckIn.ToDateTime(TimeOnly.MinValue), CheckOut.ToDateTime(TimeOnly.MinValue), null), default);

        var room = Assert.Single(rooms);
        Assert.Equal("PS-1", room.Id);
    }

    [Fact]
    public async Task FindRoom_returns_the_matching_room_from_the_named_provider()
    {
        var provider = new StubProvider(ProviderId.PremierStays,
        [
            MakeRoom(ProviderId.PremierStays, "PS-A", 100m),
            MakeRoom(ProviderId.PremierStays, "PS-B", 200m),
        ]);
        var aggregator = new HotelAggregator([provider], NullLogger<HotelAggregator>.Instance);

        var room = await aggregator.FindRoomAsync(
            ProviderId.PremierStays, "PS-B",
            new SearchQuery("Anywhere", CheckIn.ToDateTime(TimeOnly.MinValue), CheckOut.ToDateTime(TimeOnly.MinValue), null),
            default);

        Assert.NotNull(room);
        Assert.Equal("PS-B", room!.Id);
    }

    [Fact]
    public async Task FindRoom_returns_null_when_provider_absent()
    {
        var provider = new StubProvider(ProviderId.PremierStays, []);
        var aggregator = new HotelAggregator([provider], NullLogger<HotelAggregator>.Instance);

        var room = await aggregator.FindRoomAsync(
            ProviderId.BudgetNests, "BN-X",
            new SearchQuery("Anywhere", CheckIn.ToDateTime(TimeOnly.MinValue), CheckOut.ToDateTime(TimeOnly.MinValue), null),
            default);

        Assert.Null(room);
    }

    private static Room MakeRoom(ProviderId provider, string id, decimal totalPrice) =>
        new(id, provider, RoomType.Standard, PricePerNight: totalPrice / 3, TotalPrice: totalPrice,
            Currency: "USD",
            CancellationPolicy: new CancellationPolicy(CancellationPolicyType.NonRefundable, null),
            Amenities: Array.Empty<string>(),
            StarRating: null);

    private sealed class StubProvider : IHotelProvider
    {
        private readonly IReadOnlyList<Room> _rooms;
        public ProviderId Id { get; }
        public StubProvider(ProviderId id, IReadOnlyList<Room> rooms) { Id = id; _rooms = rooms; }
        public Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct) =>
            Task.FromResult(_rooms);
    }

    private sealed class ThrowingProvider : IHotelProvider
    {
        public ProviderId Id { get; }
        public ThrowingProvider(ProviderId id) { Id = id; }
        public Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct) =>
            throw new InvalidOperationException("Provider is down");
    }
}
