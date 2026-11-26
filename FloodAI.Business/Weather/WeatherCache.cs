using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FloodAI.Business.Weather;

public sealed class WeatherCache(
    IMemoryCache cache, IOptions<WeatherCacheOptions> options, TimeProvider clock, ILogger<WeatherCache> logger)
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromSeconds(OpenMeteoClient.VietnamUtcOffsetSeconds);

    // Share refreshes while keeping the number of locks fixed even if catalog coordinates change.
    private readonly SemaphoreSlim[] refreshLocks =
        Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    internal Task<(CurrentWeather Value, bool IsStale)> GetCurrentAsync(
        long areaId, double latitude, double longitude,
        Func<CancellationToken, Task<CurrentWeather>> load, CancellationToken cancellationToken) =>
        GetAsync(new CacheKey(typeof(CurrentWeather), areaId, latitude, longitude, null),
            options.Value.CurrentFreshFor, options.Value.CurrentMaxAge, load, cancellationToken);

    internal Task<(WeatherForecast Value, bool IsStale)> GetForecastAsync(
        long areaId, double latitude, double longitude,
        Func<CancellationToken, Task<WeatherForecast>> load, CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(VietnamOffset).DateTime);
        return GetAsync(new CacheKey(typeof(WeatherForecast), areaId, latitude, longitude, date),
            options.Value.ForecastFreshFor, options.Value.ForecastMaxAge, async token =>
            {
                var forecast = await load(token);
                if (forecast.Daily[0].Date != date)
                {
                    throw new WeatherException(WeatherError.ProviderUnavailable,
                        "The weather provider did not return a forecast for the current local date.");
                }
                return forecast;
            }, cancellationToken);
    }

    private async Task<(T Value, bool IsStale)> GetAsync<T>(
        CacheKey key, TimeSpan freshFor, TimeSpan maxAge,
        Func<CancellationToken, Task<T>> load, CancellationToken cancellationToken) where T : class
    {
        cache.TryGetValue<Entry<T>>(key, out var entry);
        if (ReadEntry(entry, clock.GetUtcNow()) is { } result)
        {
            return result;
        }

        var refreshLock = refreshLocks[(uint)key.GetHashCode() % (uint)refreshLocks.Length];
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            cache.TryGetValue<Entry<T>>(key, out entry);
            if (ReadEntry(entry, clock.GetUtcNow()) is { } shared)
            {
                return shared;
            }

            try
            {
                var value = await load(cancellationToken);
                var fetchedAt = clock.GetUtcNow();
                cache.Set(key, new Entry<T>(value, fetchedAt + freshFor, fetchedAt + maxAge, null), maxAge);
                return (value, false);
            }
            catch (WeatherException error) when (error.Error == WeatherError.ProviderUnavailable)
            {
                var now = clock.GetUtcNow();
                var retryAt = now + options.Value.RetryAfterFailure;
                var canUseStale = entry?.Value != null && now < entry.StaleUntil;
                logger.LogWarning(error, "Weather refresh failed for area {AreaId} and {Product}. Stale fallback: {ServedStale}.",
                    key.AreaId, key.Product.Name, canUseStale);
                if (canUseStale)
                {
                    var staleEntry = entry! with { RetryAt = retryAt };
                    var remaining = staleEntry.StaleUntil - now;
                    cache.Set(key, staleEntry, remaining > options.Value.RetryAfterFailure
                        ? remaining : options.Value.RetryAfterFailure);
                    return (staleEntry.Value!, true);
                }

                cache.Set(key, new Entry<T>(null, now, now, retryAt), options.Value.RetryAfterFailure);
                throw;
            }
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private static (T Value, bool IsStale)? ReadEntry<T>(Entry<T>? entry, DateTimeOffset now) where T : class
    {
        if (entry?.Value is { } fresh && now < entry.FreshUntil)
        {
            return (fresh, false);
        }
        if (entry?.RetryAt is { } retryAt && now < retryAt)
        {
            if (entry.Value is { } stale && now < entry.StaleUntil)
            {
                return (stale, true);
            }
            throw new WeatherException(WeatherError.ProviderUnavailable, "Open-Meteo weather is temporarily unavailable.");
        }
        return null;
    }

    private readonly record struct CacheKey(Type Product, long AreaId, double Latitude, double Longitude, DateOnly? Date);

    private sealed record Entry<T>(T? Value, DateTimeOffset FreshUntil, DateTimeOffset StaleUntil, DateTimeOffset? RetryAt);
}
