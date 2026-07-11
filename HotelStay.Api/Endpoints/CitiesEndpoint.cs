using HotelStay.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelStay.Api.Endpoints;

public static class CitiesEndpoint
{
    public static void MapCitiesEndpoint(this WebApplication app)
    {
        app.MapGet("/hotels/cities", async ([FromServices] ICityCatalogue catalog) => await catalog.GetCitiesAsync())
           .WithName("GetCities");
    }
}
