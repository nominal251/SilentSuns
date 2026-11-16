# Files to review

A few things I wasn't sure about during the cleanup. Nothing listed here was deleted.

- **Two meta files need fixing.** [terrain_beige.terrainlayer.meta](Assets/Art/Materials/Springs_Materials/terrain_beige.terrainlayer.meta) and [Ship_Exploration.unity.meta](Assets/Scenes/Gameplay/Ship_Exploration.unity.meta) still have two versions mixed together from a merge. Each has two asset IDs. We need to confirm the right one before fixing them.

- **Missing scripts in test scenes.** [DrewMinigameTest](Assets/Scenes/Tests/DrewMinigameTest.unity) has one and [XavierSampleScene](Assets/Scenes/Tests/XavierSampleScene.unity) has two. These were already missing before the cleanup. Can we check what they were supposed to do?

- **Old player setup.** DrewMinigameTest and [EnemyScene](Assets/Scenes/Tests/EnemyScene/EnemyScene.unity) still use the [old astronaut prefab](<Assets/Prefabs/Player/Astronaut Player.prefab>). EnemyScene also has the [old movement script](<Assets/Scripts/Player/Astronaut Player Script.cs>) attached separately. Do these scenes still need that setup, or should they use the updated player?

- **Two player physics materials.** All four player prefabs use [PlayerPhysicsMaterial](Assets/GameData/Player/PlayerPhysicsMaterial.physicMaterial). The copy named [Player Physics Material](<Assets/GameData/Player/Player Physics Material.physicMaterial>) has the same settings, but I couldn't find anything using it. Do we still need it?

- **Extra input files.** The Input Handler prefab uses [this file](<Assets/GameData/Player/Input/Handler/Player Input Actions.inputactions>), which includes Tool and Jump. I couldn't find references to the [other Player Input Actions file](<Assets/GameData/Player/Input/Player Input Actions.inputactions>) or [InputSystem_Actions](Assets/GameData/Player/Input/InputSystem_Actions.inputactions). The last one has UI controls, so check those before removing anything.

- **Old health-number font.** The current HUD uses the IBM font. I couldn't find references to [Readout Numerals](<Assets/UI/Fonts/Health/Readout Numerals.asset>). Check whether we want to keep it for an older version of the UI.

- **Unity welcome files.** [TutorialInfo](Assets/TutorialInfo/) and [Readme.asset](Assets/Readme.asset) are from the Unity template. Do we want to keep the welcome screen?

- **Empty wood frame folder.** The models are now in [Springs-Planet/Wood-Frames](Assets/Art/Models/Springs-Planet/Wood-Frames/). [Ice-Planet/Wood-Frames](Assets/Art/Models/Ice-Planet/Wood-Frames.meta) is empty. Is anyone still planning to use that folder?

These are things to check, not a deletion list. A file can still be used even if it didn't show up in the reference search.
