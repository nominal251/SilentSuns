# Silent Suns menu

Open `Assets/MainMenu/Scenes/MainMenu.unity` and press Play.
It loads `SampleScene` and uses the existing telescope screen. Opening `SampleScene` directly skips the intro.

## How it runs

1. `TerminalShipIntro` loads the ship, holds player input, and darkens the room.
2. `TerminalMenu` warms the screen, shows the orb, and waits for a tap.
3. `TerminalBootLog` shows the diagnostics. Then `TerminalMenu` opens the main menu.
4. Begin Survey fades the audio. `TerminalShipIntro` runs the 6.2-second screen/light glitches, moves the same camera back, and restores gameplay.
5. `TerminalPause` handles ESC and pause settings. `TerminalPauseDisplay` keeps CRT effects inside that panel.

The intro and pause menu use **unscaled time** because normal game time is stopped. Reduced Motion uses simpler fades.

## Where to change things

Paths below are inside `Assets/MainMenu`.

| Change | File or object |
| --- | --- |
| Orb messages and warm-up timing | `Scripts/TerminalMenu.cs`: `Invitations`, `WarmScreen`, `UpdateInvitation` |
| Boot text, timing, and anomalies | `Scripts/TerminalBootLog.cs`: `Entries` |
| Camera handoff and light flickers | `Scripts/TerminalShipIntro.cs`: `HandOverToGameplay`, `GetHandoffLook` |
| ESC notification | `Scripts/TerminalShipIntro.cs`: `CreatePauseHint` |
| Menu layout | `Scenes/MainMenu.unity`, under **ARCA Terminal** |
| Pause layout | `Resources/SilentSunsPause.prefab` |
| Settings controls and labels | `Scripts/TerminalSettings.cs` |
| Saved settings and defaults | `Scripts/GamePreferences.cs` |
| Scanlines, glow, and glitch strength | `Shaders/DefaultCRT.asset` |
| Menu music and its fades | **ARCA Terminal > Menu Music** |

`ConfigureShipScreen` adjusts the menu layout to fit the telescope when the scene starts.

## Connections to the game

- `TerminalDisplay` draws the UI into a texture on `Telescope/ProbeLauncherScreen`. If that object is renamed, update `TelescopeScreenPath` in `TerminalShipIntro`.
- `TerminalRoomLighting` saves and restores normal lighting. `TerminalShipScreenLight` adds the screen glow to the existing casing. Neither edits shared material assets.
- `PlayerMovement` and `InteractOrigin` check `TerminalPause.BlocksGameplayInput`. This prevents movement and instrument clicks during the intro or pause menu.
- `TerminalSettings` saves through `GamePreferences`. `GameAudio` applies the audio settings to `Resources/SilentSunsAudio.mixer`. Put music in **Music** and sound effects in **FX**.
- `ObjectiveTracker` saves the completion flag through `GameProgress`. This is not a gameplay save. Resume stays unavailable until the team connects `SessionSaveProvider`.

Pause opens without a selected button. Mouse hover highlights a button; arrows or the controller start keyboard-style selection. ESC from Settings goes back to pause.

## Before changing code

Keep public button methods and Inspector field names unless you also update their scene/prefab connections. Shader property names must match the C# calls and materials.

The graphics math lives in `TerminalDisplay` and `Shaders`. You do not need to change it to edit text, settings, or timing.

Run the **Menu** Play Mode tests in Unity's Test Runner after changes. They cover boot timing, settings, navigation, the telescope handoff, lighting restoration, and pause input. Two unrelated prototype ObjectiveTrackers have missing references; tests disable those only during testing.

## Audio credits

These are all the remaining files in `Assets/MainMenu/Audio`. Source pages were checked on October 19, 2026. CC0 below means [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/).

| File | Creator and source | License | Changes made for the game |
| --- | --- | --- | --- |
| `keypress-001.wav`, `keypress-002.wav`, `keypress-003.wav`, `keypress-004.wav` | unicaegames, [Keyboard Soundpack #1](https://opengameart.org/content/keyboard-soundpack-1-typing-and-single-keystrokes), Single Keys folder | CC0 | Copied without audio edits. |
| `Interaction/KeyboardPress.wav` | Mediasaur, [Key_Press](https://freesound.org/people/Mediasaur/sounds/788932/) | CC0 | HQ MP3 preview converted to mono 44.1 kHz, 16-bit WAV. |
| `Interaction/MouseClick.wav` | planet_jvk, [Mouse Click](https://freesound.org/people/planet_jvk/sounds/825193/) | CC0 | Same WAV conversion. |
| `Hardware/CRTPowerOn.wav` | Darklanlan, [CRT monitor turns on](https://commons.wikimedia.org/wiki/File:CRT_monitor_turns_on.ogg) | CC0 | Trimmed to 0.92–4.72 seconds, converted to mono WAV, raised 5 dB, and faded at both ends. |
| `Hardware/CRTHum.wav` | X3SB, [CRT TV Hum](https://freesound.org/people/X3SB/sounds/818513/) | CC0 | Trimmed to 1.8–6.8 seconds, converted to mono WAV, adjusted to -29 dBFS RMS, and crossfaded for looping. |
| `Menu Music.mp3` | Temporary supplied track; can be replaced later. | Not documented. | File unchanged. Fades and volume are handled in Unity. |

Exact download URLs, source hashes, and edit details are kept in `Audio/Credits.txt`, `Audio/Hardware/Sources.json`, and `Audio/Interaction/Sources.json`.

The following gameplay files were already in the project before Daniel's UI work; he did not add them. Their creators, sources, and licenses are unknown. They are kept as-is until the team confirms their credits. The planet_jvk credit above applies only to `Interaction/MouseClick.wav`.

- `Assets/Sounds/Click.mp3`, `Assets/Sounds/Click2.mp3`, `Assets/Sounds/knocking.wav`
- `Assets/Signals/WarningSignal/signal1.wav`, `Assets/Signals/signal2/signal2.mp3`, `Assets/Signals/signal3/signal3.mp3`, `Assets/Signals/signal4/signal4.mp3`

The font comes from `Assets/Fonts/Px437_DOS-V_re_ANK16.ttf`. Keep the included license files.
