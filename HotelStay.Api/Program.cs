using System.Text.Json;
using System.Text.Json.Serialization;
using HotelStay.Api.Endpoints;
using HotelStay.Api.Providers;
using HotelStay.Api.Providers.BudgetNests;
using HotelStay.Api.Providers.PremierStays;
using HotelStay.Api.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// -- Swagger / OpenAPI (dev-only UI, see below) -------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkyRoute — Hotel Stay API",
        Version = "v1",
        Description = "Aggregates two stub hotel providers (PremierStays, BudgetNests) into a unified availability + reservation surface."
    });
});

// -- Providers (IHotelProvider fan-out target) ---------------------------------
// Adding a third provider = one implementation + one line here. Spec §3.
builder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();
builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();

// -- Services -----------------------------------------------------------------
builder.Services.AddSingleton<HotelAggregator>();
builder.Services.AddSingleton<ICityCatalogue, CityCatalogue>();
builder.Services.AddSingleton<IDocumentValidator, DocumentValidator>();
builder.Services.AddSingleton<IReferenceNumberFactory, ReferenceNumberFactory>();
builder.Services.AddSingleton<IReservationStore, InMemoryReservationStore>();

// -- JSON: enums as strings, camelCase properties (defaults for ASP.NET) ------
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// -- CORS: dev-only, allow any origin so a Vite dev server on :5173 works -----
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyRoute Hotel Stay API v1");
        options.DocumentTitle = "SkyRoute — Hotel Stay API";
    });
}

app.MapSearchEndpoint();
app.MapReservationEndpoint();
app.MapCitiesEndpoint();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed so Microsoft.AspNetCore.Mvc.Testing can pick up the entry point.
public partial class Program;
