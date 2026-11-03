using System.Collections.Generic;
using UnityEngine;

namespace SilentSuns.Menu
{
    // Additive light on the existing console mesh; its original room lighting remains intact.
    public sealed class TerminalShipScreenLight : System.IDisposable
    {
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Material[]> originals = new List<Material[]>();
        private readonly List<Material> effects = new List<Material>();

        public TerminalShipScreenLight(Renderer screen, Shader shader, Texture screenTexture, Texture textLight, Vector3 screenRight, Vector3 screenUp)
        {
            if (shader == null) return;
            Vector3 up = screenUp.normalized;
            Vector3 right = screenRight.normalized;
            foreach (MeshRenderer body in screen.transform.parent.GetComponentsInChildren<MeshRenderer>())
            {
                if (body == screen) continue;
                renderers.Add(body);
                Material[] materials = body.sharedMaterials;
                originals.Add(materials);
                var replacements = new Material[materials.Length + 1];
                System.Array.Copy(materials, replacements, materials.Length);
                // Draw a light layer on the same casing. No extra monitor model is needed.
                var effect = new Material(shader);
                effect.SetTexture("_TerminalTexture", screenTexture);
                effect.SetTexture("_TextLightTexture", textLight);
                effect.SetVector("_ScreenCenter", screen.bounds.center);
                effect.SetVector("_ScreenRight", new Vector4(right.x, right.y, right.z, screenRight.magnitude * .5f));
                effect.SetVector("_ScreenUp", new Vector4(up.x, up.y, up.z, screenUp.magnitude * .5f));
                effect.SetFloat("_LightMip", Mathf.Log(screenTexture.width / 8f, 2));
                effect.SetFloat("_Power", 1);
                effects.Add(effect);
                replacements[materials.Length] = effect;
                body.sharedMaterials = replacements;
            }
        }

        public void SetPower(float power)
        {
            foreach (Material material in effects) material.SetFloat("_Power", power);
        }

        public void SetInvitation(float pulse)
        {
            foreach (Material material in effects) material.SetFloat("_Invitation", pulse);
        }

        public void Dispose()
        {
            for (int i = 0; i < renderers.Count; i++)
                if (renderers[i] != null) renderers[i].sharedMaterials = originals[i];
            foreach (Material material in effects) Object.Destroy(material);
            effects.Clear();
            renderers.Clear();
            originals.Clear();
        }
    }
}
