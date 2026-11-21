using System.IO;
using AC1AchievementsViewer.Models;
using AC1AchievementsViewer.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace AC1AchievementsViewer;

public partial class MainWindow : Window
{
    private readonly Settings settings = SettingsService.Load();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool updatingUi;                 // true while code (not the user) changes the combo boxes
    private string? currentSave;
    private DateTime currentWriteTime;
    private long currentSize;
    private Dictionary<int, AchievementText> texts = new();

    public MainWindow()
    {
        InitializeComponent();

        updatingUi = true;
        foreach (var (code, name) in TextService.Languages)
            LanguageCombo.Items.Add(new ComboBoxItem { Content = name, Tag = code });
        LanguageCombo.SelectedIndex = Math.Max(0, Array.FindIndex(TextService.Languages, l => l.Code == settings.Language));
        updatingUi = false;
        texts = TextService.Load(CurrentLanguage());

        if (string.IsNullOrEmpty(settings.SaveFolder) || !Directory.Exists(settings.SaveFolder))
            settings.SaveFolder = SaveLocator.FindDefaultFolder();

        RefreshSaveList(settings.LastSave);
        timer.Tick += (_, _) => Poll();
        timer.Start();
        Closing += (_, _) => SettingsService.Save(settings);
    }

    private string CurrentLanguage() =>
        (LanguageCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "en";

    /// <summary>Fills the save combo box from the folder (newest first) and selects wanted, or the newest.</summary>
    private void RefreshSaveList(string? wanted)
    {
        updatingUi = true;
        SaveCombo.Items.Clear();
        List<SaveEntry> saves = [];
        if (!string.IsNullOrEmpty(settings.SaveFolder) && Directory.Exists(settings.SaveFolder))
            saves = SaveLocator.ListSaves(settings.SaveFolder);
        foreach (var save in saves) SaveCombo.Items.Add(save);

        int index = saves.FindIndex(s => s.Path == wanted);
        if (index < 0 && saves.Count > 0) index = 0;
        SaveCombo.SelectedIndex = index;
        updatingUi = false;

        if (index >= 0)
            LoadSave(saves[index].Path);
        else if (currentSave == null)
        {
            SummaryText.Text = "No save selected";
            StatusLine.Text = "Use \"Open save...\" or \"Browse folder...\" to pick your Assassin's Creed save.";
        }
    }

    private void LoadSave(string path)
    {
        try
        {
            var summary = AchievementService.Build(path, texts);
            AchievementList.ItemsSource = summary.Rows;
            SummaryText.Text = $"{summary.Unlocked} / {summary.Total} unlocked    {summary.Gamerscore} / {summary.GamerscoreTotal} G";

            var info = new FileInfo(path);
            currentSave = path;
            currentWriteTime = info.LastWriteTime;
            currentSize = info.Length;
            settings.LastSave = path;
            StatusLine.Text = $"{Path.GetFileName(path)}, saved {info.LastWriteTime:g}. The list updates when the game saves.";
        }
        catch (Exception ex)
        {
            StatusLine.Text = $"Could not read {Path.GetFileName(path)}: {ex.Message}";
        }
    }

    /// <summary>Runs every 2 seconds: reloads the save if the game rewrote it, and picks up new save files.</summary>
    private void Poll()
    {
        if (currentSave == null || !File.Exists(currentSave)) return;
        try
        {
            var info = new FileInfo(currentSave);
            if (info.LastWriteTime != currentWriteTime || info.Length != currentSize)
                LoadSave(currentSave);

            if (!string.IsNullOrEmpty(settings.SaveFolder) && Directory.Exists(settings.SaveFolder))
            {
                int count = Directory.GetFiles(settings.SaveFolder, "*.sav").Length;
                if (count != SaveCombo.ItemCount) RefreshSaveList(currentSave);
            }
        }
        catch (IOException)
        {
            // The game is still writing the file; try again on the next tick.
        }
    }

    private void SaveCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (updatingUi) return;
        if (SaveCombo.SelectedItem is SaveEntry entry) LoadSave(entry.Path);
    }

    private void LanguageCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (updatingUi) return;
        settings.Language = CurrentLanguage();
        texts = TextService.Load(settings.Language);
        if (currentSave != null) LoadSave(currentSave);
    }

    private async void BrowseFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the folder that contains your .sav files",
            AllowMultiple = false
        });
        if (folders.Count == 0) return;
        settings.SaveFolder = folders[0].Path.LocalPath;
        RefreshSaveList(null);
    }

    private async void OpenSaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open an Assassin's Creed save",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("AC1 save") { Patterns = ["*.sav"] }]
        });
        if (files.Count == 0) return;
        var path = files[0].Path.LocalPath;
        settings.SaveFolder = Path.GetDirectoryName(path);
        RefreshSaveList(path);
    }
}
