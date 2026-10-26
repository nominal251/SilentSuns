# Silent Suns menu

Open `Assets/MainMenu/Scenes/MainMenu.unity` and press Play.
It loads `SampleScene` and uses the existing telescope screen. Opening `SampleScene` directly skips the intro.

## How it runs

1. `TerminalShipIntro` loads the ship, holds player input, and darkens the room.
2. `TerminalMenu` warms the screen, shows the orb, and waits for a tap.
3. `TerminalBootLog` shows the diagnostics. Then `TerminalMenu` opens the main menu.
4. Begin Survey fades the audio. `TerminalShipIntro` runs the 6.2-second screen/light glitches, moves the same camera back, and restores gameplay.
5. `TerminalPause` handles ESC and pause settings. `TerminalPauseDisplay` keeps CRT effects inside that panel.

The intro and pause menu use **unscaled time** because normal game time is stopped. Reduced Motion uses simpler fades. Both effects modes keep the 6.2-second handoff; playing before does not shorten it.

## Where to change things

Paths below are inside `Assets/MainMenu`.

| Change | File or object |
| --- | --- |
| Orb messages and warm-up timing | `Scripts/TerminalMenu.cs`: `Invitations`, `WarmScreen`, `UpdateInvitation` |
| Boot text, timing, and anomalies | `Scripts/TerminalBootLog.cs`: `Entries` |
| Camera handoff and light flickers | `Scripts/TerminalShipIntro.cs`: `HandOverToGameplay`, `PlanRecovery`, `GetHandoffLook` |
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
- `TerminalSettings` saves through `GamePreferences`. `GameAudio` applies the audio settings to `Resources/SilentSunsAudio.mixer`. Put music in **Music**, gameplay effects in **FX**, and menu sounds in **Interface**.
- `ObjectiveTracker` saves the completion flag through `GameProgress`. This is not a gameplay save. Resume stays unavailable until the team connects `SessionSaveProvider`.

`TerminalGameSession` connects the menus to `GameManager`, `ResourceManager`, and `EventManager`. The shared objects are in `Resources/SurveySystems.prefab` and survive scene changes.

- Loading the telescope menu keeps the game in Boot. Begin Survey starts a fresh run; finishing the camera handoff calls `NotifySystemLoaded`.
- Starting or restarting resets resources and event history. Returning to the menu switches back to Boot. Pause and scene transitions suspend normal event rolls.
- Xavier's `ObjectiveSystem` tracks the four planet surveys through `PlanetSurveyObjective`. `ObjectiveTracker` keeps the existing screen and cryopod connections; it reads completion from that system. Objectives wait for the intro to finish, pause with the menus, and reset on a new run. Only the registered planet surveys unlock the catalog; unrelated collection objectives do not.
- Finishing the survey marks the system complete. The current prototype submits the catalog and spends one nitrogen canister when the cryopod is used, then opens VictoryScene. Repeated submission does not add weirdness twice.
- On-foot travel spends oxygen using Drew's distance budget. The starting oxygen supply equals the player's maximum travel distance; refill zones restore it at the configured rate. Danny's ResourceManager holds the remaining supply, so the map range and game-over use the same value. Ship movement and menus do not spend suit oxygen.
- Xavier's minimap shows the player, running objectives and the last refill station's oxygen range. It appears only on foot during gameplay and hides for all menus. Screen boundaries also apply to the map. Its camera and generated textures are removed with the scene.
- Empty oxygen or hull starts the existing failure screen. Restart returns to the scene where the player failed. PlanetSurfaceTest and ObjectiveCheck use the same pause/settings controls; they remain test scenes, with no new main-menu entry.
- Controls with an `EventTrigger` fire it once per interaction and pass the player view as the event source. Triggers find the shared EventManager if none is assigned.
- Put production event definitions on the prefab's EventManager. The cube remains in EventsTesting. Save/restore and procedural system generation are still separate team work; Resume stays unavailable. `SystemLoadRequested` currently reloads SampleScene when advancing a system.

Pause opens without a selected button. Mouse hover highlights a button; arrows or the controller start keyboard-style selection. ESC from Settings goes back to pause.
Returning from gameplay opens the main-menu choices directly, without warm-up, the orb, or diagnostics. Begin Survey hides and locks the mouse pointer immediately.

## New menu features

Settings has General, Video, Audio, and Keybinds tabs. Search matches parts of words across tabs. Hover or select a control to read its description. Changes preview while editing; **Apply Settings** saves all tabs. Leaving Settings or closing the game asks to apply or discard pending changes. ESC or X cancels that question. Discard restores the previous settings, controls, and display mode.
Reset Tab changes only that tab. Display Calibration and Screen Boundaries also have their own Reset to Default buttons. Those resets are previews until applied. Boundary guides are drawn as right-angle brackets on a separate screen canvas so they stay aligned at any margin.
Keybinds has Keyboard / Mouse and Controller tabs. Each shows and resets only that device's bindings.
Controls are grouped into On Foot, In Ship, then On Foot + In Ship. Sprint is on foot only. Vertical movement, roll, and reorient are in ship only. Shared controls, including directional movement and Interact, appear only once in On Foot + In Ship.
Window mode and resolution use dropdown lists. A mode previews when selected and reverts after 15 seconds unless confirmed. Keep This Mode keeps the preview; Apply Settings saves it. VSync disables the frame-limit slider without erasing its saved value. Lists ease toward wheel input and keyboard/controller focus.

Settings and calibration temporarily turn off CRT interference, scanlines, grain, glow, and camera bloom. Brightness and contrast still apply. Closing those panels restores the saved effects; opening a calibration window from Settings keeps them off until Settings closes too.

After 60 seconds on the main menu, either pipes or a bouncing space shuttle appears. Input wakes the menu without selecting anything. The shuttle faces its movement direction. Settings and gameplay do not start the screensaver.

Hold the leave/exit confirmation for a little over a second. Release to cancel. Exit fades the audio and shuts down the terminal. Game over uses the orb with Restart Survey and Main Menu. Restart loads the prototype from the beginning; it does not restore a checkpoint.
The confirmation button fills white while held. Releasing or moving away clears the fill.

F12 saves a screenshot under the game's persistent-data folder, in `Screenshots`. The notice shows the path. Type 4725 in a menu to open build information; number-row and numpad keys both work. Search and rebinding ignore that code. Build timestamps use fixed EST (UTC-5).

| Change | Script |
| --- | --- |
| Settings rows, tabs, search, and tab defaults | `TerminalSettings` |
| Display, boundary, and controller calibration | `TerminalCalibration` |
| Saved binding overrides and conflict checks | `TerminalBindings` |
| Display mode confirmation and rollback | `TerminalDisplayOptions` |
| Mode/resolution lists and smooth scrolling | `TerminalDropdown`, `TerminalScroll` |
| Screensaver timing and shuttle movement | `TerminalScreensaver` |
| Pipe geometry | `TerminalPipes` |
| Controller notices, screenshots, build modal, shutdown | `TerminalExtras` |
| Game-over transition and choices | `TerminalFailure` |
| Modal windows and small UI creation helpers | `TerminalModal`, `TerminalUI` |

`InputHandler` makes one runtime copy of the input asset. Remapping edits that copy. Temporary duplicates are allowed. Conflicting key names turn solid red, and the side panel lists the conflicting action pairs in red, without flashing. Apply stays blocked until they are resolved. Discard restores the bindings from when Settings opened. Apply saves the overrides to PlayerPrefs. `PlayerMovement` uses the chosen look sensitivity; `InteractOrigin` reads the remappable Interact action. ESC and controller Start stay reserved for pause. F12 stays reserved for screenshots.

The pause summary shows active gameplay time for this session, excluding time in menus. It does not imply a saved game. Ship directory, physical operator guide, completion screen, and new loading screens are not included.
Visual Controls, Credits, and Screenshots are no longer listed in Settings. F12 still captures screenshots. Asset credits remain below and in the license files.

## Before changing code

Keep public button methods and Inspector field names unless you also update their scene/prefab connections. Shader property names must match the C# calls and materials.

The graphics math lives in `TerminalDisplay` and `Shaders`. You do not need to change it to edit text, settings, or timing.

Run the **Menu** Play Mode tests in Unity's Test Runner after changes. They cover boot timing, settings, navigation, the telescope handoff, lighting restoration, and pause input. The scene checks run with the real objective tracker enabled; no tracker errors are hidden by the tests.

## Audio credits

These are all the remaining files in `Assets/MainMenu/Audio`. Source pages were checked on October 19, 2026. CC0 below means [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/).

| File | Creator and source | License | Changes made for the game |
| --- | --- | --- | --- |
| `keypress-001.wav`, `keypress-002.wav`, `keypress-003.wav`, `keypress-004.wav` | unicaegames, [Keyboard Soundpack #1](https://opengameart.org/content/keyboard-soundpack-1-typing-and-single-keystrokes), Single Keys folder | CC0 | Copied without audio edits. |
| `Interaction/KeyboardPress.wav` | Mediasaur, [Key_Press](https://freesound.org/people/Mediasaur/sounds/788932/) | CC0 | HQ MP3 preview converted to mono 44.1 kHz, 16-bit WAV. |
| `Interaction/MouseClick.wav` | planet_jvk, [Mouse Click](https://freesound.org/people/planet_jvk/sounds/825193/) | CC0 | Same WAV conversion. |
| `Hardware/CRTPowerOn.wav` | Darklanlan, [CRT monitor turns on](https://commons.wikimedia.org/wiki/File:CRT_monitor_turns_on.ogg) | CC0 | Trimmed to 0.92-4.72 seconds, converted to mono WAV, raised 5 dB, and faded at both ends. |
| `Hardware/CRTHum.wav` | X3SB, [CRT TV Hum](https://freesound.org/people/X3SB/sounds/818513/) | CC0 | Trimmed to 1.8-6.8 seconds, converted to mono WAV, adjusted to -29 dBFS RMS, and crossfaded for looping. |
| `Interaction/ControllerDisconnected.ogg` | Kenney, [Interface Sounds](https://kenney.nl/assets/interface-sounds), `error_006.ogg` | CC0 | Copied unchanged. Playback volume is reduced in the game. |
| `Menu Music.mp3` | Temporary supplied track; can be replaced later. | Not documented. | File unchanged. Fades and volume are handled in Unity. |

Exact download URLs, source hashes, and edit details are kept in `Audio/Credits.txt`, `Audio/Hardware/Sources.json`, and `Audio/Interaction/Sources.json`.

The following gameplay files were already in the project before Daniel's UI work; he did not add them. Their creators, sources, and licenses are unknown. They are kept as-is until the team confirms their credits. The planet_jvk credit above applies only to `Interaction/MouseClick.wav`.

- `Assets/Sounds/Click.mp3`, `Assets/Sounds/Click2.mp3`, `Assets/Sounds/knocking.wav`
- `Assets/Signals/WarningSignal/signal1.wav`, `Assets/Signals/signal2/signal2.mp3`, `Assets/Signals/signal3/signal3.mp3`, `Assets/Signals/signal4/signal4.mp3`

The font comes from `Assets/Fonts/Px437_DOS-V_re_ANK16.ttf`, VileR's [Oldschool PC Font Pack](https://int10h.org/oldschool-pc-fonts/readme/), CC BY-SA 4.0. The existing font file is unchanged.

The shuttle is [Font Awesome Free shuttle-space](https://fontawesome.com/icons/shuttle-space), by Fonticons, Inc., [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The SVG was converted to a white PNG; the screensaver tints and rotates it. The copyright comment remains in the SVG. License copies are in `Licenses`.

`TerminalPolishBuild` updates shared UI assets and build metadata before a player build. It does not rebuild the menu scene. Use **Silent Suns > Main Menu > Update shared UI assets** if adding or replacing shared artwork/audio.
