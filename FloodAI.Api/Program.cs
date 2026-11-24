using FloodAI.Api.Commands;
using FloodAI.Business.Catalog;
using FloodAI.Business.Observations;
using FloodAI.DataAccess;
using FloodAI.DataAccess.Observations;
using Microsoft.EntityFrameworkCore;

var importVanUc = args is [VanUcObservationCommand.Name, ..];
var builder = WebApplication.CreateBuilder(importVanUc ? [] : args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddScoped<HydrologyCatalogService>();
builder.Services.AddScoped<ObservationService>();
builder.Services.AddScoped<VanUcObservationStore>();
builder.Services.AddDbContext<FloodAiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FloodAI")));

var app = builder.Build();
if (importVanUc)
{
    await using (app)
    {
        Environment.ExitCode = await VanUcObservationCommand.RunAsync(app.Services, args[1..]);
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

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
