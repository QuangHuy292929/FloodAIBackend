using FloodAI.Api.Authentication;
using FloodAI.Api.Commands;
using FloodAI.Api.Community;
using FloodAI.Api.Middleware;
using FloodAI.Api.OpenApi;
using FloodAI.Business.Accounts;
using FloodAI.Business.Catalog;
using FloodAI.Business.Community;
using FloodAI.Business.Forecasting;
using FloodAI.Business.Observations;
using FloodAI.DataAccess;
using FloodAI.DataAccess.Entities;
using FloodAI.DataAccess.Observations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var command = args.FirstOrDefault();
var runCommand = command is VanUcObservationCommand.Name or VanUcForecastRegistrationCommand.Name or PromoteAdminCommand.Name;
var builder = WebApplication.CreateBuilder(runCommand ? [] : args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ForecastExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("FloodAI");
if (builder.Configuration["Auth:DataProtectionKeysPath"] is { Length: > 0 } keysPath)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<PasswordResetTokens>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PasswordResetEmailSender>();
builder.Services.AddAuthentication(AccountBearerHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AccountBearerHandler>(AccountBearerHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddScoped<HydrologyCatalogService>();
builder.Services.AddScoped<CommunityPostService>();
builder.Services.AddScoped<CommunityCommentsService>();
builder.Services.AddScoped<CommunityMediaService>();
builder.Services.AddSingleton<ICommunityMediaStore, LocalCommunityMediaStore>();
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
    options.AddPolicy(HistoricalForecastApiConvention.RateLimitPolicy, PerClientRateLimit);
    options.AddPolicy(AccountBearerHandler.RateLimitPolicy, PerClientRateLimit);
    options.AddPolicy(CommunityRequests.WritePolicy, PerClientRateLimit);
});
builder.Services.AddDbContext<FloodAiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FloodAI")));

var app = builder.Build();
if (runCommand)
{
    await using (app)
    {
        Environment.ExitCode = command switch
        {
            VanUcObservationCommand.Name => await VanUcObservationCommand.RunAsync(app.Services, args[1..]),
            VanUcForecastRegistrationCommand.Name => await VanUcForecastRegistrationCommand.RunAsync(app.Services, args[1..]),
            _ => await PromoteAdminCommand.RunAsync(app.Services, args[1..])
        };
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
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static RateLimitPartition<string> PerClientRateLimit(HttpContext context) =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
