using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AC1AchievementsViewer.Models;

/// <summary>One line of the achievement list, already filled with everything the screen shows.</summary>
public class AchievementRow
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public bool Completed { get; init; }
    public Bitmap? Picture { get; init; }

    /// <summary>Locked achievements are drawn dimmer.</summary>
    public double PictureOpacity => Completed ? 1.0 : 0.4;

    /// <summary>True when the bar is shown (collectibles and counters).</summary>
    public bool HasProgress { get; init; }
    public double ProgressMax { get; init; } = 1;
    public double ProgressValue { get; init; }

    /// <summary>For example "37 / 100". Can be set without a bar (only a count is known).</summary>
    public string ProgressText { get; init; } = "";
    public bool HasProgressText => ProgressText.Length > 0;

    public string GamerscoreText { get; init; } = "";
    public string StatusText => Completed ? "Unlocked" : "Locked";
    public IBrush StatusBrush => Completed ? Brushes.LimeGreen : Brushes.Gray;
}
