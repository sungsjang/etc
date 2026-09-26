using System.Globalization;
using System.Text.Json;
using UsdKrwWidget.Models;

namespace UsdKrwWidget.Services;

internal sealed class TwelveDataClient
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly string _apiKey;

    public TwelveDataClient(string? apiKey = null)
    {
        _apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? Environment.GetEnvironmentVariable("TWELVE_DATA_API_KEY")
                ?? throw new InvalidOperationException(
                    "TWELVE_DATA_API_KEY environment variable is not set.")
            : apiKey.Trim();
    }

    public async Task<decimal> GetCurrentRateAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.twelvedata.com/price?symbol=USD/KRW&apikey={Uri.EscapeDataString(_apiKey)}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        ThrowIfApiError(document.RootElement);

        var priceText = document.RootElement.GetProperty("price").GetString();
        if (!decimal.TryParse(priceText, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
            throw new InvalidDataException("Twelve Data returned an invalid USD/KRW price.");

        return price;
    }

    public async Task<IReadOnlyList<RatePoint>> GetRecentFiveMinuteRatesAsync(
        int outputSize = 2200,
        CancellationToken cancellationToken = default)
    {
        outputSize = Math.Clamp(outputSize, 1, 5000);
        var url = "https://api.twelvedata.com/time_series" +
                  $"?symbol=USD/KRW&interval=5min&outputsize={outputSize}&timezone=Asia/Seoul" +
                  $"&apikey={Uri.EscapeDataString(_apiKey)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        ThrowIfApiError(document.RootElement);

        var result = new List<RatePoint>();
        if (!document.RootElement.TryGetProperty("values", out var values))
            return result;

        foreach (var value in values.EnumerateArray())
        {
            var timestampText = value.GetProperty("datetime").GetString();
            var closeText = value.GetProperty("close").GetString();

            if (DateTime.TryParse(timestampText, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var timestamp) &&
                decimal.TryParse(closeText, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var close))
            {
                result.Add(new RatePoint(timestamp, close));
            }
        }

        return result.OrderBy(x => x.Timestamp).ToArray();
    }

    private static void ThrowIfApiError(JsonElement root)
    {
        if (root.TryGetProperty("status", out var status) &&
            string.Equals(status.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var message = root.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "Unknown Twelve Data API error.";
            throw new InvalidOperationException(message);
        }
    }
}
