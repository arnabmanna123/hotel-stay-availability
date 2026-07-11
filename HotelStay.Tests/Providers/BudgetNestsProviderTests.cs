using HotelStay.Api.Domain;
using HotelStay.Api.Providers.BudgetNests;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelStay.Tests.Providers;

public class BudgetNestsProviderTests
{
    private static BudgetNestsProvider Create() =>
        new(NullLogger<BudgetNestsProvider>.Instance);

    [Fact]
    public async Task Filters_out_available_false_rooms()
    {
        var provider = Create();
        var query = new SearchQuery("London", new DateTime(2026, 8, 1), new DateTime(2026, 8, 4), null);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        // BN London stub has 3 rooms — 2 available, 1 unavailable. Only available survive.
        Assert.Equal(2, rooms.Count);
        Assert.DoesNotContain(rooms, r => r.Id == "BN-LON-STD-002");
    }

    [Fact]
    public async Task Returns_empty_when_all_matching_rooms_are_unavailable()
    {
        var provider = Create();
        var query = new SearchQuery("Tokyo", new DateTime(2026, 8, 1), new DateTime(2026, 8, 4), null);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        // Tokyo BN entries are all `available: false`.
        Assert.Empty(rooms);
    }

    [Fact]
    public async Task Parses_snake_case_JSON_correctly()
    {
        var provider = Create();
        var query = new SearchQuery("Paris", new DateTime(2026, 8, 1), new DateTime(2026, 8, 4), RoomType.Standard);

        var rooms = await provider.SearchAsync(query, CancellationToken.None);

        var room = Assert.Single(rooms);
        Assert.Equal("BN-PAR-STD-001", room.Id);
        Assert.Equal(RoomType.Standard, room.RoomType);
        Assert.Equal(90m, room.PricePerNight);
    }

    [Fact]
    public void Normalise_maps_Flexible_to_24_hours()
    {
        var dto = new BudgetNestsRoomDto
        {
            Id = "BN-X",
            Destination = "Anywhere",
            RoomType = "standard",
            PricePerNight = 60m,
            Cancellation = "Flexible",
            Available = true,
        };

        var room = BudgetNestsProvider.Normalise(dto, nights: 2);

        Assert.Equal(CancellationPolicyType.Flexible, room.CancellationPolicy.Type);
        Assert.Equal(24, room.CancellationPolicy.HoursBeforeCheckIn);
    }

    [Fact]
    public void Normalise_omits_amenities_and_starRating()
    {
        var dto = new BudgetNestsRoomDto
        {
            Id = "BN-X",
            Destination = "Anywhere",
            RoomType = "standard",
            PricePerNight = 60m,
            Cancellation = "NonRefundable",
            Available = true,
        };

        var room = BudgetNestsProvider.Normalise(dto, nights: 1);

        Assert.Empty(room.Amenities);
        Assert.Null(room.StarRating);
    }
}
