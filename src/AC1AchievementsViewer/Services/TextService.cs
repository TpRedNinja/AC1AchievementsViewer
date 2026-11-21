using System.IO;
using System.Text;
using Avalonia.Platform;

namespace AC1AchievementsViewer.Services;

/// <summary>Title and description of one achievement in one language.</summary>
public record AchievementText(string Title, string Description);

/// <summary>
/// Reads the achievement texts from the language files that ship with the AC1 achievements mod
/// (AC1Ach_en.ini and the others). They are built into the program, so nothing has to be installed.
/// The files have sections like [00] with Title= and Description= lines (slot number = section number).
/// </summary>
public static class TextService
{
    public static readonly (string Code, string Name)[] Languages =
    [
        ("en", "English"), ("fr", "Français"), ("es", "Español"), ("de", "Deutsch"),
        ("it", "Italiano"), ("pl", "Polski"), ("ru", "Русский")
    ];

    public static Dictionary<int, AchievementText> Load(string languageCode)
    {
        var result = Read(languageCode);
        // A missing language file or missing entries fall back to English.
        if (languageCode != "en")
            foreach (var pair in Read("en"))
                result.TryAdd(pair.Key, pair.Value);
        return result;
    }

    private static Dictionary<int, AchievementText> Read(string languageCode)
    {
        var result = new Dictionary<int, AchievementText>();
        try
        {
            var uri = new Uri($"avares://AC1AchievementsViewer/Assets/Languages/AC1Ach_{languageCode}.ini");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            int slot = -1;
            string title = "", description = "";
            void Finish()
            {
                if (slot >= 0) result[slot] = new AchievementText(title, description);
                title = "";
                description = "";
            }

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == ';') continue;

                if (line[0] == '[' && line[^1] == ']')
                {
                    Finish();
                    slot = int.TryParse(line.AsSpan(1, line.Length - 2), out var n) ? n : -1;
                }
                else if (line.StartsWith("Title=", StringComparison.OrdinalIgnoreCase))
                    title = line[6..];
                else if (line.StartsWith("Description=", StringComparison.OrdinalIgnoreCase))
                    description = line[12..];
            }
            Finish();
        }
        catch
        {
            // Nothing readable: the caller falls back to English or to the names inside ACSaveLib.
        }
        return result;
    }
}
