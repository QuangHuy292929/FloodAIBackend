using System.Globalization;
using System.Text;
using FloodAI.Business.Observations;
using FloodAI.DataAccess.Observations;
using Microsoft.VisualBasic.FileIO;
using Npgsql;

namespace FloodAI.Api.Commands;

internal static class VanUcObservationCommand
{
    public const string Name = "--import-van-uc";

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        var dryRun = args is [_, "--dry-run"];
        long trungTrangId = 0;
        long honDauId = 0;
        if (!dryRun && (args is not [_, "--trung-trang-id", var trungTrang, "--hon-dau-id", var honDau] ||
            !long.TryParse(trungTrang, NumberStyles.None, CultureInfo.InvariantCulture, out trungTrangId) ||
            !long.TryParse(honDau, NumberStyles.None, CultureInfo.InvariantCulture, out honDauId) ||
            trungTrangId <= 0 || honDauId <= 0 || trungTrangId == honDauId))
        {
            Console.Error.WriteLine(
                "Usage: --import-van-uc <csv> (--dry-run | --trung-trang-id <id> --hon-dau-id <id>)");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        void Cancel(object? sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        }

        Console.CancelKeyPress += Cancel;
        try
        {
            var rows = VanUcCsvReader.Read(args[0], cancellation.Token);
            var count = rows.Count * 3;
            if (dryRun)
            {
                Console.WriteLine($"Validated {rows.Count} consecutive hours and {count} values.");
                return 0;
            }

            await using var scope = services.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<VanUcObservationStore>();
            var inserted = await store.ImportAsync(rows, trungTrangId, honDauId, cancellation.Token);
            Console.WriteLine($"Validated {rows.Count} hours. Inserted {inserted} observations; {count - inserted} already existed.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Import cancelled.");
            return 130;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidDataException or MalformedLineException or DecoderFallbackException or InvalidOperationException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
        catch (PostgresException error)
        {
            Console.Error.WriteLine($"PostgreSQL rejected the import (SQLSTATE {error.SqlState}).");
            return 1;
        }
        catch (NpgsqlException)
        {
            Console.Error.WriteLine("Unable to connect to PostgreSQL or complete the import.");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= Cancel;
        }
    }
}
