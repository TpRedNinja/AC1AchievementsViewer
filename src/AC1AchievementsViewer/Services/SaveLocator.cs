using System.IO;
using AcSaveLib.Games.Ac1;

namespace AC1AchievementsViewer.Services;

/// <summary>One .sav file in the saves folder.</summary>
public class SaveEntry
{
    public string Path { get; init; } = "";
    public DateTime LastWrite { get; init; }
    public int? Unlocked { get; init; }

    /// <summary>The name the player gave the save in game (from the .hdr file next to the .sav), or null.</summary>
    public string? GameName { get; init; }

    /// <summary>The in-game name from the .hdr file; the file name only when there is no readable .hdr.</summary>
    public string Display =>
        !string.IsNullOrWhiteSpace(GameName) ? GameName : System.IO.Path.GetFileNameWithoutExtension(Path);
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

    /// <summary>
    /// The .hdr next to the save holds an int32 character count followed by that many UTF-16 characters
    /// (bytes 03 00 00 00 41 00 6E 00 79 00 = "Any"). It is found by the number after the dash in the file name.
    /// </summary>
    private static string? ReadGameName(string savePath)
    {
        try
        {
            var name = Path.GetFileNameWithoutExtension(savePath);
            int dash = name.LastIndexOf('-');
            if (dash < 0) return null;
            string ending = name[dash..] + ".hdr";   // "-1774900138.hdr"
            var folder = Path.GetDirectoryName(Path.GetFullPath(savePath))!;

            foreach (var hdr in Directory.EnumerateFiles(folder))
            {
                if (!hdr.EndsWith(ending, StringComparison.OrdinalIgnoreCase)) continue;
                var bytes = File.ReadAllBytes(hdr);
                if (bytes.Length < 6) continue;

                int count = BitConverter.ToInt32(bytes, 0);
                string text = count > 0 && 4 + count * 2 <= bytes.Length
                    ? System.Text.Encoding.Unicode.GetString(bytes, 4, count * 2)
                    : System.Text.Encoding.Unicode.GetString(bytes, 4, (bytes.Length - 4) / 2 * 2);
                text = text.TrimEnd('\0').Trim();
                if (text.Length > 0) return text;
            }
        }
        catch
        {
            // No readable .hdr: the list falls back to the file name.
        }
        return null;
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
                result.Add(new SaveEntry { Path = file, LastWrite = File.GetLastWriteTime(file), Unlocked = CountUnlocked(file), GameName = ReadGameName(file) });
        }
        catch
        {
            // Folder missing or not readable: an empty list is the answer.
        }
        result.Sort((a, b) => b.LastWrite.CompareTo(a.LastWrite));
        return result;
    }
}
