using System.Text.Json;
using UsdKrwWidget.Models;

namespace UsdKrwWidget.Services;

internal sealed class RateCache
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public RateCache()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "UsdKrwWidget");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "rates.json");
    }

    public async Task<List<RatePoint>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new List<RatePoint>();

        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<RatePoint>>(stream, _jsonOptions)
                   ?? new List<RatePoint>();
        }
        catch
        {
            return new List<RatePoint>();
        }
    }

    public async Task SaveAsync(IEnumerable<RatePoint> points)
    {
        var cutoff = DateTime.Now.AddDays(-30);
        var compact = points
            .Where(x => x.Timestamp >= cutoff)
            .GroupBy(x => x.Timestamp)
            .Select(g => g.Last())
            .OrderBy(x => x.Timestamp)
            .ToList();

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, compact, _jsonOptions);
    }
}
