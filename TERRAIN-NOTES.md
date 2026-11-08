# Terrain files

Terrain `.asset` files contain binary data. The old Git rules treated them as text and removed bytes that looked like line endings. The rules now preserve those bytes and keep terrain files out of text merges. Normal YAML assets still work as text.

- Bring the `.gitattributes` fix into your branch before committing terrain changes. It also needs to be in the shared main branch.
- If you have terrain work that has not been committed, close Unity and copy the terrain files and their `.meta` files somewhere outside the repository first. Keep that backup until the scene has been checked.
- A corrupted file needs a good backup or a verified repair. Pulling the rule change alone will not repair it. Do not copy an older broken file over a repaired one.
- Keep each terrain file with its existing `.meta` file so scene references stay connected.
- If two branches change the same terrain, choose the correct complete terrain file together. Do not text-merge it.
- Before sharing terrain changes, run `python Tools/check_unity_binary_assets.py` from the repository folder and open the scene in Unity. The same check runs on GitHub pushes and pull requests once this workflow is on the branch. It checks file sizes and Git handling; it does not replace checking the terrain in Unity.

The repair to `acid redo data 5.asset` restores two missing bytes and keeps its existing GUID. All 2,730 stored height bounds match. Unity loaded all six terrain assets and the AcidSprings scene, and 125 terrain collision checks passed. The repair was reconstructed from the surviving data; Bella should still check the sculpting against what she expects.
