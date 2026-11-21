namespace AC1AchievementsViewer.Models;

/// <summary>What the viewer remembers between runs.</summary>
public class Settings
{
    /// <summary>The folder that holds the .sav files.</summary>
    public string? SaveFolder { get; set; }

    /// <summary>The save that was open last time.</summary>
    public string? LastSave { get; set; }

    /// <summary>Language code of the texts: en, fr, es, de, it, pl or ru.</summary>
    public string Language { get; set; } = "en";
}
