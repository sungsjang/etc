using System.Text.Json;

namespace UsdKrwWidget.Services;

internal sealed class AppSettings
{
    public int? X { get; set; }
    public int? Y { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool ShowWeek { get; set; }

    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UsdKrwWidget");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            await using var stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(DirectoryPath);
        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
