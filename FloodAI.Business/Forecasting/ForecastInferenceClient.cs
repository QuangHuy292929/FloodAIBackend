using System.Net.Http.Json;
using System.Text.Json;

namespace FloodAI.Business.Forecasting;

public sealed class ForecastInferenceClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions ResponseJson = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true
    };

    public Task<InferenceBundle> GetBundleAsync(CancellationToken cancellationToken) =>
        ReadResponseAsync<InferenceBundle>(
            () => httpClient.GetAsync("bundle", cancellationToken), cancellationToken);

    public Task<InferenceResult> PredictAsync(
        DateTime issueTimeLocal,
        string manifestHash,
        IReadOnlyList<ForecastHour> history,
        CancellationToken cancellationToken) =>
        ReadResponseAsync<InferenceResult>(
            () => httpClient.PostAsJsonAsync("predict",
                new { issueTimeLocal, manifestHash, history }, cancellationToken), cancellationToken);

    private static async Task<T> ReadResponseAsync<T>(
        Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await send();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ResponseJson, cancellationToken)
                ?? throw new JsonException("Empty inference response.");
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ForecastException(ForecastError.Unavailable, "The inference service timed out.", error);
        }
        catch (Exception error) when (error is HttpRequestException or JsonException or NotSupportedException)
        {
            throw new ForecastException(ForecastError.Unavailable,
                "The inference service did not return a usable response.", error);
        }
    }
}
