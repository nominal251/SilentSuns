# Files to review

These came up during the folder cleanup. Everything listed here is still in the project.

- **Old astronaut prefab and script.** [DrewMinigameTest](Assets/Scenes/Tests/DrewMinigameTest.unity) and [EnemyScene](Assets/Scenes/Tests/EnemyScene/EnemyScene.unity) both use the [old astronaut prefab](<Assets/Prefabs/Player/Astronaut Player.prefab>). EnemyScene also uses the [old movement script](<Assets/Scripts/Player/Astronaut Player Script.cs>) directly. Do these tests still need the old controller? If we swap players, check movement in both scenes and the script on the EnemyScene object.
- **Player physics materials.** Both player prefabs use [PlayerPhysicsMaterial](Assets/GameData/Player/PlayerPhysicsMaterial.physicMaterial). [Player Physics Material](<Assets/GameData/Player/Player Physics Material.physicMaterial>), the one with spaces, has the same settings. No direct references turned up for that copy. That’s the one to review.
- **Player input files.** The [Input Handler prefab](<Assets/Prefabs/Player/Input Handler.prefab>) uses the [13-action file](<Assets/GameData/Player/Input/Handler/Player Input Actions.inputactions>), which includes Tool and Jump. No direct references turned up for the [11-action file](<Assets/GameData/Player/Input/Player Input Actions.inputactions>). Check whether it has any bindings or control schemes we still need.
- **Menu input.** [InputSystem_Actions](Assets/GameData/Player/Input/InputSystem_Actions.inputactions) is the only one of the three input files with a UI action map. No direct references turned up for it either. Check which bindings the menus use for keyboard, controller and mouse before removing it.
- **Unity welcome screen.** [TutorialInfo](Assets/TutorialInfo/) and [Readme.asset](Assets/Readme.asset) came with the Unity template. The welcome screen uses both. Do we still want it?
- **Empty wood frame folder.** [Springs-Planet/Wood-Frames](Assets/Art/Models/Springs-Planet/Wood-Frames.meta) is empty. Is anyone planning to use it? The [Ice-Planet folder](Assets/Art/Models/Ice-Planet/Wood-Frames/) has the actual models.

The reference check covered project files and settings stored as text. Unity can also load assets by name or through binary files, so a search can miss a use.
