# Acid terrain repair and team instructions

Repair branch: `dan-acid-fix` in `nominal251/SilentSuns`.
Scene: `Assets/Scenes/AcidSprings.unity`.
Verified with Unity `6000.5.10f1` on November 8, 2026.

This branch is based on Bella's `acid-3`. Publishing the repair branch does not update `acid-3`, `main_new`, or `dev_ui`. Follow the integration order below before treating this as a project-wide fix.

## What went wrong

Three separate problems were involved:

1. **Binary terrain files were treated as text.** The old `.gitattributes` rules applied text line-ending handling to `.asset` files. Terrain data contains binary bytes, including occasional `0D 0A` pairs that happen to look like Windows line endings. Conversion removed `0D` bytes and made the files shorter than their own headers. The current terrain 5 was two bytes short. Comparing its two committed versions confirms that one more line-ending conversion removed the second byte. Its first committed version was already damaged, so the history does not establish the exact first conversion on someone's machine. This is not evidence that Bella sculpted it incorrectly.
2. **The sixth terrain tile was not placed in the scene.** `acid redo data 6.asset` was intact and flat, but nothing in the scene used it. The scene had five tiles and an empty corner. It was already like that when the rebuilt terrain was first committed; the corruption repair did not remove it.
3. **An older merge revert affects model integration.** Bella's branch history contains a merge of Hope's art followed by a revert. Xavier's concern has merit for missing models: those deletions can enter another branch during a merge. This is separate from the binary terrain corruption. Git remembers the original merge even after it is reverted, so merging Hope's unchanged branch again does not necessarily restore the deleted files. Git does not simply choose whichever commit has the latest date.

## What this branch fixes

- Terrain 5 has its two missing bytes restored. The repaired heightmap matches all 2,730 stored minimum/maximum height values. Its existing `.meta` and GUID are preserved. This was a reconstruction checked against surviving data, not recovery from an untouched original backup.
- The scene now includes `acid redo 6`, using the existing sixth terrain asset and a matching TerrainCollider at `(800, 0, -800)`. It sits under `acid planet terrain` and uses the same material and terrain settings as the neighboring flat tile. Its existing flat heightmap was not resculpted.
- `.gitattributes` protects terrain and other covered binary Unity data from text conversion and text merging. Text/YAML assets still remain readable and mergeable.
- `Tools/check_unity_binary_assets.py` checks binary asset integrity and Git handling.
- `.github/workflows/unity-binary-assets.yml` runs that check on GitHub pushes and pull requests.

## Validation and limits

Unity loaded all six terrain assets and all six placed scene tiles. The tiles occupy the expected six positions with no duplicate slot. Both new terrain neighbor connections are reciprocal. All 1,026 height comparisons along the two joins passed, along with 150 collision checks across the tiles and 204 collision checks beside the joins. Unity-rendered overhead and angled views show the previously missing corner filled in.

The terrain asset data and existing GUIDs were not changed by adding the sixth scene object. These are editor scene, rendering, and physics checks, not a full game playthrough or Windows build. Bella should still review the recovered sculpting against her intended design. The sixth tile is deliberately flat because that matches the existing asset and her description.

This does not repair every old terrain asset in the repository. A separate read-only audit of `main_new` at `6744fc1` found 38 terrain assets with file-size/header mismatches among 65 recognized binary assets. Some may be obsolete, but that needs a reference and owner review. They were not deleted or repaired as part of this branch.

## Integration order

### 1. Everyone: protect work that exists only on your computer

Save and close Unity before switching or merging branches. Copy uncommitted terrain assets, their `.meta` files, and the scene you edited to a folder outside the repository. Include any new materials, textures, models, or other dependencies created for that work. Keep the backup until the merged scene has been checked.

Do not stage or commit new terrain under the old Git rules just to make the working tree clean. Ask Xavier to help bring the protection rules into that branch first, and check the staged result. Do not discard uncommitted work or overwrite it with the repair files.

### 2. Bella: bring the repair into `acid-3` and review it

After protecting local work, fetch the repository and merge `origin/dan-acid-fix` into `acid-3`. In GitHub Desktop, this means select `acid-3`, fetch, and merge the repair branch into the current branch. Do not merge unrelated branches at the same time.

At the time of this fix, `acid-3` is `d01dd2b` and the repair branch extends it directly. If newer local or remote terrain work exists, preserve and compare it before merging; do not resolve a binary conflict by blindly accepting one side.

Open AcidSprings with the editor version in `ProjectSettings/ProjectVersion.txt`. Confirm all six tiles are present, terrain 5's sculpting is correct, and terrain 6 fills the previously empty corner. Walk over the restored ground and both joins in the team's normal gameplay setup. Run the Python check below. Push the reviewed `acid-3` result when satisfied.

### 3. Xavier: bring the protection and scene into main carefully

Start an integration branch from the latest `main_new` with a clean working tree. First cherry-pick protection commit `2540f1b22a6b50e6d04eca610cc4cacdd6c1bdc8`. It contains the Git rules, checker, workflow, and initial notes without importing Bella's old merge-revert history. Preserve any newer `.gitattributes` rules when resolving a conflict.

Run the checker on that integration branch. Review the older damaged terrain files it reports: restore or repair files that are still used, and only remove obsolete files after checking scene/prefab references and confirming with their owners. Changing the Git rules cannot restore bytes that are already missing.

Then integrate Bella's reviewed AcidSprings scene and terrain work, including its needed materials, models, and `.meta` files. Review the merge diff for deleted art assets. Preserve Hope's required assets and existing GUIDs; do not blindly accept the old revert's deletions or reverse the entire old revert over newer work. Selective transfer or a carefully resolved merge may be needed. The repair branch is not a drop-in replacement for all of `main_new`.

Run the asset checker and verify the affected scenes in Unity before merging into `main_new`. Once the existing failures are resolved, make the workflow's `check-assets` job a required PR check in the repository's branch protection/ruleset if available. This branch adds the check but does not configure that requirement.

### 4. Everyone: update your own branch after main is verified

Fetch and merge the repaired `main_new` into your own working branch before sharing more terrain changes. Fetching alone only downloads commits; it does not put `.gitattributes` or the fixed assets into your branch. Check the scene after merging. If someone must continue terrain work before main is ready, coordinate with Xavier to bring the protection commit into that branch first.

## The automated check

From the repository root, using Python 3 (the GitHub workflow uses Python 3.12):

```text
python Tools/check_unity_binary_assets.py
```

On Windows, `py -3 Tools/check_unity_binary_assets.py` is an alternative if Python is installed through the launcher. No extra Python packages are needed.

The checker reads tracked `.asset` and `.lighting` files under `Assets`. For recognized Unity serialized binary files it checks:

- File length against the length stored in the Unity header.
- Whether Git's staging filters would change the binary bytes.
- Whether Git attributes allow unsafe text conversion or text merging.

It prints the failing file and reason and exits with an error if a check fails. The current repair branch checks seven binary assets with zero errors. Stage newly added assets before running it so they are included; never do that under the old unsafe rules.

On GitHub, open **Actions > Check Unity binary assets** or the PR's checks. The workflow runs on branches that contain it. It does not repair files, open Unity, enforce branch protection by itself, verify model/LFS downloads, or detect every kind of corruption. YAML files and Git LFS pointer files are skipped. A green check also cannot establish that a designer's intended tile exists in the scene; that requires the Unity scene review.

## Rules for future terrain work

- Keep `.gitattributes`, the checker, and the workflow when merging. Do not restore the old blanket text handling for `.asset` files.
- Keep terrain assets with their original `.meta` files. Renaming a file or regenerating its `.meta` is not a corruption fix and can break scene references. Use Unity's Project window for moves and renames so metadata follows the asset.
- Coordinate ownership of terrain tiles. If two branches edit the same binary terrain, compare the complete versions with the designer and choose/rebuild the intended result in Unity. Binary terrain cannot be merged like script text.
- Add an explicit `binary` rule for new binary asset paths that are not already covered. Avoid marking every `.asset` binary, because many other Unity assets are text/YAML and benefit from normal diffs and merges.
- Keep Git LFS installed for the project's LFS-managed models, textures, and other assets. Check that new clones actually download those files. This terrain fix does not replace the existing LFS setup.
- Do not restore old broken terrain over a repaired file just because Git asks which version to keep. Check the bytes and the scene first.
- Do not use mass renormalization, file renaming, deleting `Library`, or changing editor serialization settings as a substitute for repairing damaged binary data. Clearing Unity's cache does not put missing bytes back into the source file.
- The committed attributes are the shared protection; setting `core.autocrlf` on one person's computer is not a team-wide fix. No global Git setting change is required by this repair.
- If the check fails, stop before merging the damaged files into a shared branch. Keep the failing file and its `.meta`, report the path and check output, and work from a backup or verified repair.
