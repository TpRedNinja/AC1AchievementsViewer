using System.IO;
using AC1AchievementsViewer.Models;
using AcSaveLib.Games.Ac1;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AC1AchievementsViewer.Services;

/// <summary>What the window shows for one save.</summary>
public class SaveSummary
{
    public List<AchievementRow> Rows { get; init; } = [];
    public int Unlocked { get; init; }
    public int Total { get; init; }
    public int Gamerscore { get; init; }
    public int GamerscoreTotal { get; init; }
}

/// <summary>Turns a .sav file (read by ACSaveLib) into the rows of the achievement list.</summary>
public static class AchievementService
{
    /// <summary>The mod's own final achievement ("Nothing is True...") is slot 44 in the language files and pictures.</summary>
    private const int FinalSlot = 44;

    /// <summary>Slots the Xbox 360 hid until they were unlocked (their texts are the story texts).</summary>
    private static readonly HashSet<int> SecretSlots = [1, 8, 9, 10, 11, 12, 13, 14, 15, 16, 41, 42, 43];

    /// <summary>Counter achievements: slot, how many are needed. The save stores the running count.</summary>
    private static readonly Dictionary<int, int> CounterNeeded = new()
    {
        [24] = 100,   // Eagle's Will: defeat 100 opponents without dying
        [26] = 100,   // Eagle's Prey: assassinate 100 guards
        [29] = 100,   // Eagle's Swiftness: 100 counter kills
        [30] = 50,    // Eagle's Dive: 50 combo kills
        [31] = 50,    // Eagle's Talon: 50 stealth assassinations
        [32] = 50,    // Eagle's Dance: 50 leaps of faith
        [33] = 200,   // The hands of a Thief: pickpocket 200 throwing daggers
        [34] = 20,    // March of the Pious: monk blending 20 times
        [35] = 75,    // Eagle's Eye: 75 guards killed with thrown daggers
        [36] = 25,    // Enemy of the Poor: grab and throw 25 harassers
        [37] = 20     // Gifted Escapist: 20 merchant stands
    };

    /// <summary>Achievements that collect indices in a list: slot, how many are needed.</summary>
    private static readonly Dictionary<int, int> ListNeeded = new()
    {
        [17] = 91,    // Fearless: all reach high points
        [19] = 28,    // Defender of the People: Acre
        [20] = 25,    // Defender of the People: Jerusalem
        [21] = 30     // Defender of the People: Damascus
    };

    /// <summary>Hungerer of Knowledge counts memory glitches seen; only the count is shown, no bar.</summary>
    private const int GlitchSlot = 18;

    private static readonly Dictionary<int, Bitmap?> PictureCache = [];

    public static SaveSummary Build(string savePath, Dictionary<int, AchievementText> texts)
    {
        var save = Ac1SaveFile.Load(savePath);
        var accomplishments = save.Accomplishments()
            .Where(a => a.Config == 1)
            .ToDictionary(a => a.Slot);
        var collectibles = save.Collectibles().ToDictionary(c => c.Slot);

        var rows = new List<AchievementRow>();
        int unlocked = 0, gamerscore = 0;
        int gamerscoreTotal = Ac1Achievements.All.Sum(a => a.Gamerscore);

        foreach (var info in Ac1Achievements.All)
        {
            accomplishments.TryGetValue(info.Slot, out var state);
            bool completed = state != null && state.Completed;
            if (completed)
            {
                unlocked++;
                gamerscore += info.Gamerscore;
            }

            texts.TryGetValue(info.Slot, out var text);
            string title = text?.Title ?? info.Title;
            string description = text?.Description ?? info.Requirement;
            if (SecretSlots.Contains(info.Slot) && !completed)
            {
                title = "Secret achievement";
                description = "Keep playing to find out.";
            }

            // Progress: collectible lists, then counters, then index lists.
            bool hasBar = false;
            int max = 1, value = 0;
            string progressText = "";
            if (collectibles.TryGetValue(info.Slot, out var collectible))
            {
                hasBar = true;
                max = collectible.Needed;
                value = collectible.Collected;
            }
            else if (CounterNeeded.TryGetValue(info.Slot, out var counterMax))
            {
                hasBar = true;
                max = counterMax;
                value = (int)(state?.Count ?? 0);
            }
            else if (ListNeeded.TryGetValue(info.Slot, out var listMax))
            {
                hasBar = true;
                max = listMax;
                value = state?.UsedEvents.Count ?? 0;
            }
            else if (info.Slot == GlitchSlot)
            {
                progressText = $"{state?.Count ?? 0} seen";
            }

            if (hasBar)
            {
                if (completed) value = max;
                value = Math.Min(value, max);
                progressText = $"{value} / {max}";
            }

            rows.Add(new AchievementRow
            {
                Title = title,
                Description = description,
                Completed = completed,
                Picture = LoadPicture(info.Slot),
                HasProgress = hasBar,
                ProgressMax = max,
                ProgressValue = value,
                ProgressText = progressText,
                GamerscoreText = $"{info.Gamerscore} G"
            });
        }

        // The mod's final achievement: unlocked once all 44 others are.
        bool finalDone = unlocked == Ac1Achievements.All.Count;
        texts.TryGetValue(FinalSlot, out var finalText);
        rows.Add(new AchievementRow
        {
            Title = finalText?.Title ?? "Nothing is True, Everything is Permitted",
            Description = finalText?.Description ?? "Unlock all other achievements.",
            Completed = finalDone,
            Picture = LoadPicture(FinalSlot),
            HasProgress = true,
            ProgressMax = Ac1Achievements.All.Count,
            ProgressValue = unlocked,
            ProgressText = $"{unlocked} / {Ac1Achievements.All.Count}",
            GamerscoreText = "mod only"
        });

        return new SaveSummary
        {
            Rows = rows,
            Unlocked = unlocked + (finalDone ? 1 : 0),
            Total = rows.Count,
            Gamerscore = gamerscore,
            GamerscoreTotal = gamerscoreTotal
        };
    }

    private static Bitmap? LoadPicture(int slot)
    {
        if (PictureCache.TryGetValue(slot, out var cached)) return cached;
        Bitmap? bitmap = null;
        try
        {
            var uri = new Uri($"avares://AC1AchievementsViewer/Assets/Pictures/{slot:00}.png");
            using var stream = AssetLoader.Open(uri);
            bitmap = new Bitmap(stream);
        }
        catch
        {
            // No picture for this slot: the row just has no image.
        }
        PictureCache[slot] = bitmap;
        return bitmap;
    }
}
