using System.Globalization;
using FloodAI.Business.Forecasting;
using Npgsql;

namespace FloodAI.Api.Commands;

internal static class VanUcForecastRegistrationCommand
{
    public const string Name = "--register-van-uc-forecast";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        if (args is not [var station] ||
            !long.TryParse(station, NumberStyles.None, CultureInfo.InvariantCulture, out var stationId) ||
            stationId <= 0)
        {
            Console.Error.WriteLine("Usage: --register-van-uc-forecast <Trung Trang station id>");
            return 2;
        }

        await using var scope = services.CreateAsyncScope();
        try
        {
            var registration = scope.ServiceProvider.GetRequiredService<ForecastModelRegistration>();
            await registration.RegisterAsync(stationId, CancellationToken.None);
            Console.WriteLine("Verified the Văn Úc models for 1h, 3h and 6h and the raw Q95 research threshold.");
            return 0;
        }
        catch (ForecastException error)
        {
            Console.Error.WriteLine(error.Message);
        }
        catch (NpgsqlException)
        {
            Console.Error.WriteLine("PostgreSQL rejected or could not complete the registration.");
        }
        return 1;
    }
}
