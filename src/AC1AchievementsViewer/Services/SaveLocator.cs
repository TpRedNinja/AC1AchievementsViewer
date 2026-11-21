using System.IO;
using AcSaveLib.Games.Ac1;

namespace AC1AchievementsViewer.Services;

/// <summary>One .sav file in the saves folder.</summary>
public class SaveEntry
{
    public string Path { get; init; } = "";
    public DateTime LastWrite { get; init; }
    public int? Unlocked { get; init; }

    /// <summary>
    /// The save file holds no name, only numbers. Its file name looks like "45220571-1774900138.sav";
    /// the second number is a Unix time (when the save was created), so that is shown instead of the cryptic name.
    /// </summary>
    public string Display
    {
        get
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);
            string text = name;
            int dash = name.LastIndexOf('-');
            if (dash >= 0 && long.TryParse(name[(dash + 1)..], out var unix) && unix > 1_000_000_000 && unix < 4_000_000_000)
                text = $"Started {DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime:g}";
            text += $"   last saved {LastWrite:g}";
            if (Unlocked != null) text += $"   {Unlocked}/44";
            return text;
        }
    }
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

    private static int? CountUnlocked(string file)
    {
        try
        {
            return Ac1SaveFile.Load(file).Accomplishments().Count(a => a.Config == 1 && a.Completed);
        }
        catch
        {
            return null;   // not an AC1 save, or the game is writing it right now
        }
    }

    /// <summary>All .sav files of a folder, newest first.</summary>
    public static List<SaveEntry> ListSaves(string folder)
    {
        var result = new List<SaveEntry>();
        try
        {
            foreach (var file in Directory.GetFiles(folder, "*.sav"))
                result.Add(new SaveEntry { Path = file, LastWrite = File.GetLastWriteTime(file), Unlocked = CountUnlocked(file) });
        }
        catch
        {
            // Folder missing or not readable: an empty list is the answer.
        }
        result.Sort((a, b) => b.LastWrite.CompareTo(a.LastWrite));
        return result;
    }
}
