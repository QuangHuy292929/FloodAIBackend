using FloodAI.Api.Commands;
using FloodAI.Api.Middleware;
using FloodAI.Api.OpenApi;
using FloodAI.Business.Catalog;
using FloodAI.Business.Forecasting;
using FloodAI.Business.Observations;
using FloodAI.DataAccess;
using FloodAI.DataAccess.Observations;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var command = args.FirstOrDefault();
var runCommand = command is VanUcObservationCommand.Name or VanUcForecastRegistrationCommand.Name;
var builder = WebApplication.CreateBuilder(runCommand ? [] : args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ForecastExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddScoped<HydrologyCatalogService>();
builder.Services.AddScoped<ObservationService>();
builder.Services.AddScoped<VanUcObservationStore>();
builder.Services.AddScoped<VanUcForecastObservations>();
builder.Services.AddScoped<HistoricalForecastService>();
builder.Services.AddScoped<ForecastModelRegistration>();
builder.Services.AddHttpClient<ForecastInferenceClient>(client =>
{
    var baseUrl = builder.Configuration["ForecastInference:BaseUrl"]
        ?? throw new InvalidOperationException("ForecastInference:BaseUrl is required.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(HistoricalForecastApiConvention.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddDbContext<FloodAiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FloodAI")));

var app = builder.Build();
if (runCommand)
{
    await using (app)
    {
        Environment.ExitCode = command == VanUcObservationCommand.Name
            ? await VanUcObservationCommand.RunAsync(app.Services, args[1..])
            : await VanUcForecastRegistrationCommand.RunAsync(app.Services, args[1..]);
    }
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
