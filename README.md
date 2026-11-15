# LET'S GO FOR VROUVES / ΠΑΜΕ ΓΙΑ ΒΡΟΥΒΕΣ (Android)

The cute Cretan tower defence for Android phones and tablets, written in C# with .NET 10
for Android and built with Visual Studio 2026. It is a port of the Amstrad CPC game in
`../VrouvesCPC`: the same ten stages, waves, defenders (the Lyra player too), invaders and
rules, with the cartoon graphics, sounds and music of the PC version (`../VrouvesPC`).

Armed with EU Directive 8647/2026, Frankenstein & Moreau Co. send bio-drones, compliance
guards and weed-destroying tanks to wipe out the wild, "copyrighted" *vrouves* of Crete.
The villagers of Zoniana defend their sacks of bitter greens, helped by the defected drone
Psssst.

## Building

### In Visual Studio 2026

It needs the **.NET Multi-platform App UI development** workload (it brings .NET for
Android, the Android SDK and the Microsoft OpenJDK).

1. Clone or copy the repository to a folder on the Windows side (for example
   `C:\src\VrouvesAndroid`). A Debug build also works straight from
   `\\wsl.localhost\Ubuntu\home\vasilhs\repos\VrouvesAndroid`, but a Release build fails
   there with *Failed to extract debug info*: the native libraries cannot be stripped on a
   WSL path.
2. Open `Vrouves.slnx`, make **Vrouves** the startup project, pick a phone (USB debugging
   on) or an emulator from the Android device list, and press F5.
3. For a release: set the configuration to Release and use **Build > Archive** (or
   `dotnet publish`). The APK and the Google Play bundle (`.aab`) are signed with the
   debug key unless an `AndroidSigningKeyStore` is set; Google Play needs your own key.

### From WSL

```bash
tools/build.sh Release
```

It mirrors the sources to `%LOCALAPPDATA%\Temp\VrouvesAndroid-build`, builds them there
with the Windows `dotnet` of Visual Studio 2026 (`tools\build.cmd`, which also works on its
own from a Windows prompt) and copies `Vrouves-Release.apk` and `Vrouves-Release.aab` to
`build/`. `tools/build.sh` alone makes `build/Vrouves-Debug.apk` (with its assemblies inside,
so it runs without Visual Studio's fast deployment). To install on a phone with USB
debugging on:

```bash
"/mnt/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe" install -r build/Vrouves-Release.apk
```

The app runs on Android 7.0 (API 24) and later, in landscape.

### In the emulator

```bash
tools/run_emulator.sh            # or: tools/run_emulator.sh Debug
```

starts the AVD `Vrouves_Pixel7` (a Pixel 7 with Android 16, x86_64, accelerated by
Windows' WHPX), installs `build/Vrouves-Release.apk` and opens the game. The emulator and
its system image are in a separate user SDK, `%LOCALAPPDATA%\Android\Sdk`, because the one
Visual Studio installed in Program Files needs administrator rights to add packages;
Visual Studio lists the running emulator among its devices all the same.

For testing, the extras `--ei stage N` (a game from stage N, with three lives; add
`--ez bot true` and the stage plays by itself, as in the store screenshots) and
`--ei ending SCORE` (the ending, then the high score entry) can follow, as the PC
version's `--stage` and `--ending`:

```bash
tools/run_emulator.sh Release --ei stage 8
```

A Debug build logs its frame rate and drawing time every three seconds
(`adb logcat -s VrouvesPerf`).

## Playing

| Touch | Action |
| --- | --- |
| A defender card on the right | Choose it (a "?" card joins in a later stage) |
| Hold a finger on a tile | See the defender's range: the ring and the tile are green where it can go, red where not. Slide to another tile if you like |
| Lift the finger | Build there |
| A defender, then the same one again | The first touch says what it sells for, the second sells it (half price back) |
| Next wave | Call the next wave now (half the seconds left are paid as a bonus) |
| Speed | Game speed x1 / x2 / x3 |
| Pause, or Back | Pause; Back twice quits to the title |

A keyboard or game pad works too: arrows / Q A O P move the cursor, SPACE / ENTER / A
builds, S / DEL / X sells, 1-7 or L1 / R1 choose a defender, N / Y calls the next wave,
F speeds up, H / START pauses, L switches English / Greek, M music, ESC is Back.

On the title screen: Start, How to play (the story, the defenders, the invaders and the
controls), High scores, the difficulty (lives for the whole game: Easy 4, Medium 3, Hard 2,
Hardest none), Ελληνικά / English and the music. The game starts in English. A game that ends among the eight best scores asks for a three-letter name.
The settings and the high scores are kept in the app's SharedPreferences.

The rules are the CPC ones; `../VrouvesPC/docs/cpc_mechanics.md` has every number (25 logic
ticks a second, path steps, ranges, armour, neighbour bonuses, score, lives).

## How it is made

* `Vrouves/Game/` is the game without Android: `Data.cs` (the invader and defender tables,
  the neighbour bonuses, the texts in English and Greek, the stages) and `Stage.cs` (one
  stage, tick by tick: waves, invaders on their paths, defenders, shots, effects, building,
  selling, neighbour bonuses, score). It is VrouvesPC's `scripts/data.gd` and
  `scripts/game.gd` in C#, which follow the CPC's `src/game.asm`.
* `Vrouves/Android/` is the app around it, with no engine: one `View` (`GameView.cs`) that
  draws on its Canvas every frame and scales the screens to fill the display, keeping
  clear of any camera cutout; the screens (`TitleScreen.cs`, `GameScreen.cs`,
  `EndingScreen.cs`), the flow of a game (`App.cs`), drawing helpers (`Gfx.cs`), sound with
  SoundPool and MediaPlayer (`Sfx.cs`) and the activity (`MainActivity.cs`: full screen,
  landscape, Back, pausing when the app goes to the background).
* The game screen is as tall as the 20 × 14 map plus the top bar and as wide as the
  phone: the map on the left (64 units a cell), the defender cards, the info on the
  chosen defender or the tile under the finger, and the buttons in a panel on the right.
* `tools/import_assets.sh` copies the stage data, sprites, sounds, fonts and icon from
  `../VrouvesPC` into `Vrouves/Assets` and `Vrouves/Resources` (the stage pictures and the
  title and ending pictures as JPEG, the tune as Ogg Vorbis).

## Testing

```bash
dotnet run -c Release --project tests/Sim plan
```

plays every stage headless with the CPC's build plans (`Vrouves/Assets/data/plans.json`, from
`VrouvesCPC/tools/scenario.py`); all ten are won. Without `plan` it plays VrouvesWEB's
simple greedy build order (`tests/autoplay.gd`), which loses from stage 2 on, as the Godot
version does with the same order. Stage numbers after the arguments play only those.

## Credits

REVIVE8BIT - 2026 - VASPER. Noto Sans fonts: SIL Open Font License
(`Vrouves/Assets/fonts/LICENSE-noto.txt`).
