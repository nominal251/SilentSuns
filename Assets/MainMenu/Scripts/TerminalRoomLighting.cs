using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SilentSuns.Menu
{
    // Saves the room's normal lighting so the intro can dim it and put it back afterward.
    public sealed class TerminalRoomLighting : System.IDisposable
    {
        private readonly Scene scene;
        private readonly List<Light> lamps = new List<Light>();
        private readonly List<float> intensities = new List<float>();
        private readonly List<Surface> surfaces = new List<Surface>();
        private readonly AmbientMode ambientMode;
        private readonly Color ambientLight, sky, equator, ground;
        private readonly float ambientIntensity, reflectionIntensity;
        private readonly SphericalHarmonicsL2 ambientProbe;
        private readonly bool fog;
        private readonly Camera camera;
        private readonly CameraClearFlags clearFlags;
        private readonly Color background;
        private bool disposed;
        public float Level { get; private set; }

        private sealed class Surface
        {
            public Renderer renderer;
            public int slot;
            // A property block changes this object without editing the shared material asset.
            public MaterialPropertyBlock saved, working;
            public bool hasBase, hasEmission;
            public Color baseColor, emission;
        }

        public TerminalRoomLighting(Renderer screen, Camera playerCamera)
        {
            scene = screen.gameObject.scene;
            camera = playerCamera;
            clearFlags = camera.clearFlags;
            background = camera.backgroundColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            ambientMode = RenderSettings.ambientMode;
            ambientLight = RenderSettings.ambientLight;
            sky = RenderSettings.ambientSkyColor;
            equator = RenderSettings.ambientEquatorColor;
            ground = RenderSettings.ambientGroundColor;
            ambientIntensity = RenderSettings.ambientIntensity;
            reflectionIntensity = RenderSettings.reflectionIntensity;
            ambientProbe = RenderSettings.ambientProbe;
            fog = RenderSettings.fog;

            SaveLights();
            SaveSurfaces(screen);
            SetLevel(0);
        }

        private void SaveLights()
        {
            foreach (Light lamp in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (lamp.gameObject.scene != scene)
                    continue;
                lamps.Add(lamp);
                intensities.Add(lamp.intensity);
            }
        }

        private void SaveSurfaces(Renderer screen)
        {
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer == screen || renderer.gameObject.scene != scene) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || (!material.HasProperty("_BaseColor") && !material.HasProperty("_EmissionColor"))) continue;
                    var surface = new Surface
                    {
                        renderer = renderer,
                        slot = i,
                        saved = new MaterialPropertyBlock(),
                        working = new MaterialPropertyBlock(),
                        hasBase = material.HasProperty("_BaseColor"),
                        hasEmission = material.HasProperty("_EmissionColor")
                    };
                    renderer.GetPropertyBlock(surface.saved, i);
                    renderer.GetPropertyBlock(surface.working, i);
                    if (surface.hasBase) surface.baseColor = surface.saved.HasColor("_BaseColor") ? surface.saved.GetColor("_BaseColor") : material.GetColor("_BaseColor");
                    if (surface.hasEmission) surface.emission = surface.saved.HasColor("_EmissionColor") ? surface.saved.GetColor("_EmissionColor") : material.GetColor("_EmissionColor");
                    surfaces.Add(surface);
                }
            }
        }

        // TerminalShipIntro passes 0 for a dark room and 1 for normal gameplay lighting.
        public void SetLevel(float amount)
        {
            if (disposed) return;
            Level = Mathf.Clamp01(amount);
            for (int i = 0; i < lamps.Count; i++)
            {
                if (lamps[i] != null)
                    lamps[i].intensity = intensities[i] * Level;
            }
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight * Level;
            RenderSettings.ambientSkyColor = sky * Level;
            RenderSettings.ambientEquatorColor = equator * Level;
            RenderSettings.ambientGroundColor = ground * Level;
            RenderSettings.ambientIntensity = ambientIntensity * Level;
            RenderSettings.reflectionIntensity = reflectionIntensity * Level;
            RenderSettings.ambientProbe = ambientProbe * Level;
            RenderSettings.fog = false;
            foreach (Surface surface in surfaces)
            {
                if (surface.renderer == null) continue;
                if (surface.hasBase)
                {
                    Color tint = surface.baseColor * Level;
                    tint.a = surface.baseColor.a;
                    surface.working.SetColor("_BaseColor", tint);
                }
                if (surface.hasEmission) surface.working.SetColor("_EmissionColor", surface.emission * Level);
                surface.renderer.SetPropertyBlock(surface.working, surface.slot);
            }
        }

        // Called when the intro ends, or if it gets interrupted by a scene change.
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Level = 1;
            for (int i = 0; i < lamps.Count; i++)
            {
                if (lamps[i] != null)
                    lamps[i].intensity = intensities[i];
            }
            foreach (Surface surface in surfaces)
                if (surface.renderer != null) surface.renderer.SetPropertyBlock(surface.saved.isEmpty ? null : surface.saved, surface.slot);
            // A newly loaded scene already has its own ambient settings.
            if (SceneManager.GetActiveScene() == scene)
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                RenderSettings.ambientIntensity = ambientIntensity;
                RenderSettings.reflectionIntensity = reflectionIntensity;
                RenderSettings.ambientProbe = ambientProbe;
                RenderSettings.fog = fog;
            }
            if (camera != null)
            {
                camera.clearFlags = clearFlags;
                camera.backgroundColor = background;
            }
        }
    }
}
