# AC1 Achievements Viewer

A small desktop program (Avalonia, .NET 8, Windows and Linux) that shows your Assassin's Creed 1
achievements by reading the game's `.sav` files with [ACSaveLib](https://github.com/TpRedNinja/ACSaveLib).

- Lists all 44 achievements plus the mod's final one, with pictures, texts in 7 languages, gamerscore and progress bars.
- Finds your saves in `%APPDATA%\Ubisoft\Assassin's Creed\Saved Games` automatically (or pick a folder / file).
- Reloads by itself when the game writes the save. The save only changes when the game saves, so progress shows up after a save, not live.
- Not shown: the best time for the 10-minute achievement (not stored in the save).

## Build

```
git clone --recurse-submodules https://github.com/TpRedNinja/AC1AchievementsViewer.git
```
If you already cloned without the flag: `git submodule update --init`.
Open `AC1AchievementsViewer.sln` in Visual Studio, restore NuGet packages, run.

## Credits

- Achievement pictures and language files come from the AC1 achievements mod. The achievement texts are
  based on the Assassin's Creed Fandom wiki and are licensed CC BY-SA 3.0 (see the headers of the files in `Assets/Languages`).
- Parts of this project were written with the help of an AI assistant (Claude).

## License

MIT for the code (see LICENSE). The language files keep their CC BY-SA 3.0 license.
