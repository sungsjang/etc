using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using UsdKrwWidget.Models;

namespace UsdKrwWidget.Services;

internal sealed class NaverFinanceClient
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public NaverFinanceClient()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UsdKrwWidget", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<decimal> GetCurrentRateAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://api.stock.naver.com/marketindex/exchange/FX_USDKRW";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        if (!root.TryGetProperty("exchangeInfo", out var info))
            throw new InvalidDataException("Naver Finance returned an unexpected exchange response.");

        var priceText = info.TryGetProperty("closePrice", out var closePrice)
            ? closePrice.GetString()
            : null;

        if (!TryParseRate(priceText, out var price))
            throw new InvalidDataException("Naver Finance returned an invalid USD/KRW price.");

        return price;
    }

    public async Task<IReadOnlyList<RatePoint>> GetRecentDailyRatesAsync(
        int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 7, 100);
        var url = "https://m.stock.naver.com/front-api/marketIndex/prices" +
                  $"?category=exchange&reutersCode=FX_USDKRW&page=1&pageSize={pageSize}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        if (!root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Naver Finance returned an unexpected history response.");

        var points = new List<RatePoint>();
        foreach (var item in result.EnumerateArray())
        {
            var dateText = item.TryGetProperty("localTradedAt", out var localTradedAt)
                ? localTradedAt.GetString()
                : null;
            var priceText = item.TryGetProperty("closePrice", out var closePrice)
                ? closePrice.GetString()
                : null;

            if (TryParseNaverDate(dateText, out var timestamp) && TryParseRate(priceText, out var rate))
                points.Add(new RatePoint(timestamp, rate));
        }

        return points
            .OrderBy(p => p.Timestamp)
            .ToArray();
    }

    private static bool TryParseRate(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return decimal.TryParse(
            text.Replace(",", string.Empty, StringComparison.Ordinal),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool TryParseNaverDate(string? text, out DateTime value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var formats = new[]
        {
            "yyyy-MM-dd'T'HH:mm:sszzz",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy.MM.dd"
        };

        return DateTime.TryParseExact(
            text,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out value)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    }
}
