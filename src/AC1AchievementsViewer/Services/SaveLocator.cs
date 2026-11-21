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

    /// <summary>Which .hdr was read, or why none was (shown in the status line to find problems).</summary>
    public string NameNote { get; init; } = "";

    /// <summary>The in-game name with the number of unlocked achievements. Only the file name if no name was found.</summary>
    public string Display
    {
        get
        {
            string text = !string.IsNullOrWhiteSpace(GameName) ? GameName : System.IO.Path.GetFileNameWithoutExtension(Path);
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

    /// <summary>
    /// The save name is in the .hdr with the same file name as the save ("1774900138.sav" -> "1774900138.hdr").
    /// It holds an int32 character count followed by that many UTF-16 characters (03 00 00 00 41 00 6E 00 79 00 = "Any").
    /// </summary>
    private static string? ReadGameName(string savePath, out string note)
    {
        note = "no .hdr found";
        try
        {
            var hdr = Path.ChangeExtension(savePath, ".hdr");
            if (!File.Exists(hdr)) { note = $"{Path.GetFileName(hdr)} does not exist"; return null; }

            var bytes = File.ReadAllBytes(hdr);
            if (bytes.Length < 6) { note = $"{Path.GetFileName(hdr)} is too short"; return null; }

            int count = BitConverter.ToInt32(bytes, 0);
            if (count <= 0 || 4 + count * 2 > bytes.Length) { note = $"{Path.GetFileName(hdr)} has a bad length"; return null; }

            var text = System.Text.Encoding.Unicode.GetString(bytes, 4, count * 2).Trim('\0', ' ');
            note = $"name from {Path.GetFileName(hdr)}";
            return text.Length > 0 ? text : null;
        }
        catch (Exception ex)
        {
            note = $"could not read .hdr: {ex.Message}";
            return null;
        }
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
            {
                var gameName = ReadGameName(file, out var note);
                result.Add(new SaveEntry { Path = file, LastWrite = File.GetLastWriteTime(file), Unlocked = CountUnlocked(file), GameName = gameName, NameNote = note });
            }
        }
        catch
        {
            // Folder missing or not readable: an empty list is the answer.
        }
        result.Sort((a, b) => b.LastWrite.CompareTo(a.LastWrite));
        return result;
    }
}
