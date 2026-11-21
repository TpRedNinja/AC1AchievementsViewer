using System.IO;

namespace AC1AchievementsViewer.Services;

/// <summary>One .sav file in the saves folder.</summary>
public class SaveEntry
{
    public string Path { get; init; } = "";
    public DateTime LastWrite { get; init; }
    public string Display => $"{System.IO.Path.GetFileName(Path)}   ({LastWrite:g})";
}

/// <summary>Finds the folder with the game's saves and lists the saves in it.</summary>
public static class SaveLocator
{
    private const string SteamAppId = "15100";   // Assassin's Creed on Steam

    /// <summary>The first default location that exists, or null (then the user browses for it).</summary>
    public static string? FindDefaultFolder()
    {
        foreach (var folder in Candidates())
            if (Directory.Exists(folder)) return folder;
        return null;
    }

    private static IEnumerable<string> Candidates()
    {
        // Windows: %APPDATA%\Ubisoft\Assassin's Creed\Saved Games
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
            yield return Path.Combine(appData, "Ubisoft", "Assassin's Creed", "Saved Games");

        // Linux: the Wine prefix Steam makes for the game (Proton)
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) yield break;
        var inPrefix = Path.Combine("pfx", "drive_c", "users", "steamuser", "AppData", "Roaming",
                                    "Ubisoft", "Assassin's Creed", "Saved Games");
        string[] steamRoots =
        [
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam")
        ];
        foreach (var root in steamRoots)
            yield return Path.Combine(root, "steamapps", "compatdata", SteamAppId, inPrefix);
    }

    /// <summary>All .sav files of a folder, newest first.</summary>
    public static List<SaveEntry> ListSaves(string folder)
    {
        var result = new List<SaveEntry>();
        try
        {
            foreach (var file in Directory.GetFiles(folder, "*.sav"))
                result.Add(new SaveEntry { Path = file, LastWrite = File.GetLastWriteTime(file) });
        }
        catch
        {
            // Folder missing or not readable: an empty list is the answer.
        }
        result.Sort((a, b) => b.LastWrite.CompareTo(a.LastWrite));
        return result;
    }
}
