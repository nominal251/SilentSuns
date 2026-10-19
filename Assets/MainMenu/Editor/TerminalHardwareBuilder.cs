using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilentSuns.Menu.Editor
{
    public static class TerminalHardwareBuilder
    {
        private const string Folder = "Assets/MainMenu/Hardware";

        // The enclosure is modeled geometry. Its edges and recesses receive the room lighting.
        public static TerminalHardware Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/MainMenu", "Hardware");
            var root = new GameObject("Terminal Hardware");
            var hardware = root.AddComponent<TerminalHardware>();
            Material paint = Material("Chassis", new Color(0.16f, 0.17f, 0.155f), 0.05f, 0.24f);
            paint.shader = Shader.Find("Silent Suns/Terminal Bezel");
            paint.SetFloat("_SpillStrength", 10f);
            paint.SetFloat("_ReflectionStrength", .32f);
            paint.SetFloat("_TextScatter", 8f);
            Material rear = Material("Rear shell", new Color(0.10f, 0.112f, 0.105f), 0.02f, 0.18f);
            Material dark = Material("Recess", new Color(0.013f, 0.016f, 0.017f), 0f, 0.2f);
            Material steel = Material("Fasteners", new Color(0.18f, 0.19f, 0.18f), 0.72f, 0.3f);
            Material glass = Material("Unlit glass", new Color(0.009f, 0.014f, 0.015f), 0.08f, 0.76f);
            Material key = Material("Power key", new Color(0.085f, 0.095f, 0.087f), 0f, 0.3f);
            Material led = Material("Power indicator", new Color(0.012f, 0.018f, 0.01f), 0f, 0.45f);
            led.EnableKeyword("_EMISSION");
            led.SetColor("_EmissionColor", new Color(.7f,.025f,.008f));
            led.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            // Profiles run from the glass aperture, up the inner reveal, across the face and into the body.
            Mesh bezel = Ring("Bezel", new[] {
                new Vector4(8.005f, 4.505f, 0.54f, 0.08f),
                new Vector4(8.09f, 4.59f, 0.36f, 0.09f),
                new Vector4(8.22f, 4.72f, -0.22f, 0.10f),
                new Vector4(8.68f, 5.28f, -0.22f, 0.12f),
                new Vector4(8.82f, 5.42f, -0.08f, 0.16f),
                new Vector4(8.82f, 5.42f, 0.52f, 0.16f) });
            var front = Model(root.transform, "Front enclosure", bezel, paint);
            Model(root.transform, "Housing seam", Ring("Seam", new[] {
                new Vector4(8.81f,5.41f,0.52f,0.16f), new Vector4(8.81f,5.41f,0.56f,0.16f) }), dark);
            Model(root.transform, "Rear enclosure", Ring("Rear", new[] {
                new Vector4(8.82f,5.42f,0.56f,0.16f), new Vector4(8.80f,5.40f,0.70f,0.16f),
                new Vector4(7.76f,4.5f,3.4f,0.23f) }), rear);
            Box(root.transform, "Back cover", new Vector3(0,0,3.38f), new Vector3(15.5f,8.95f,0.16f), rear);
            Mesh glassMesh = ConvexGlass();
            var glassBody = Model(root.transform, "Display glass", glassMesh, glass);
            glassBody.transform.localPosition = new Vector3(0,0,.57f);
            Material coating = Material("Glass coating", new Color(.002f,.003f,.004f,.016f), 0f, .72f);
            coating.shader = Shader.Find("Silent Suns/Terminal Glass");
            coating.SetFloat("_Reflection", .045f);
            coating.renderQueue = 3010;
            var glassCoating = Model(root.transform, "Glass reflection", glassMesh, coating);
            glassCoating.transform.localPosition = new Vector3(0,0,.53f);
            glassCoating.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            Box(root.transform, "Power socket", new Vector3(6.85f,-5.02f,-0.265f), new Vector3(2.22f,0.49f,0.12f), dark);
            var power = Box(root.transform, "Power switch", new Vector3(6.85f,-5.02f,-0.34f), new Vector3(2.08f,0.36f,0.12f), key);
            var indicator = Primitive(root.transform, "Power LED", PrimitiveType.Sphere, new Vector3(5.37f,-5.02f,-0.265f), new Vector3(0.105f,0.105f,0.04f), led);
            for (int i = 0; i < 16; i++)
                Box(root.transform, "Vent " + i, new Vector3(-6.6f + i * 0.16f,-5.05f,-0.227f), new Vector3(0.045f,0.25f,0.012f), dark);
            foreach (float x in new[] {-8.52f,8.52f})
                foreach (float y in new[] {-4.95f,4.95f})
                {
                    var screw = Primitive(root.transform, "Captive screw", PrimitiveType.Cylinder, new Vector3(x,y,-0.24f), new Vector3(0.13f,0.018f,0.13f), steel);
                    screw.transform.localRotation = Quaternion.Euler(90,0,0);
                    var slot = Box(root.transform, "Screw slot", new Vector3(x,y,-0.263f), new Vector3(0.075f,0.014f,0.008f), dark);
                    slot.transform.localRotation = Quaternion.Euler(0,0,17);
                }
            Box(root.transform, "Mount", new Vector3(0,-5.6f,1.4f), new Vector3(4.8f,0.5f,2.8f), dark);
            Material deck = Material("Desk", new Color(0.025f,0.031f,0.034f), 0.3f, 0.27f);
            Box(root.transform, "Console deck", new Vector3(0,-5.91f,2.5f), new Vector3(35,0.16f,19), deck);
            Box(root.transform, "Compartment wall", new Vector3(0,1.5f,6), new Vector3(40,25,0.2f), dark);

            Light keyLight = new GameObject("Cabin work light", typeof(Light)).GetComponent<Light>();
            keyLight.transform.SetParent(root.transform);
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f,0.93f,0.82f);
            keyLight.intensity = .18f;
            keyLight.transform.rotation = Quaternion.Euler(32,-28,0);
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.8f;
            keyLight.shadowBias = 0.08f;
            keyLight.shadowNormalBias = 0.1f;
            keyLight.cullingMask = (1 << 30) | (1 << 29);
            Light fill = new GameObject("Cabin bounce", typeof(Light)).GetComponent<Light>();
            fill.transform.SetParent(root.transform);
            fill.transform.position = new Vector3(5,3,-6);
            fill.type = LightType.Point;
            fill.range = 22;
            fill.intensity = .07f;
            fill.color = new Color(0.48f,0.6f,0.72f);
            fill.cullingMask = (1 << 30) | (1 << 29);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.006f,.008f,.011f);
            RenderSettings.ambientEquatorColor = new Color(.003f,.004f,.006f);
            RenderSettings.ambientGroundColor = new Color(.001f,.0015f,.002f);
            RenderSettings.reflectionIntensity = 0.22f;

            var fields = new SerializedObject(hardware);
            fields.FindProperty("indicator").objectReferenceValue = indicator.GetComponent<Renderer>();
            fields.FindProperty("powerSwitch").objectReferenceValue = power.transform;
            fields.FindProperty("bezel").objectReferenceValue = front.GetComponent<Renderer>();
            fields.FindProperty("glassCoating").objectReferenceValue = glassCoating.GetComponent<Renderer>();
            fields.ApplyModifiedPropertiesWithoutUndo();
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
                child.gameObject.layer = 30;
            glassBody.layer = glassCoating.layer = 29;
            return hardware;
        }

        private static Material Material(string name, Color color, float metallic, float smoothness)
        {
            string path = Folder + "/" + name + ".mat";
            Material value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value == null)
            {
                value = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(value, path);
            }
            value.SetColor("_BaseColor", color);
            value.SetFloat("_Metallic", metallic);
            value.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(value);
            return value;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            return Primitive(parent, name, PrimitiveType.Cube, position, size, material);
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static GameObject Model(Transform parent, string name, Mesh mesh, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static Mesh ConvexGlass()
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            int columns = TerminalGlassGraphic.Columns, rows = TerminalGlassGraphic.Rows;
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    float u = (float)x / columns, v = (float)y / rows;
                    vertices.Add(new Vector3((u-.5f)*16f, (v-.5f)*9f, TerminalGlassGraphic.Depth(u*2f-1f,v*2f-1f,TerminalGlassGraphic.Bow)));
                    uv.Add(new Vector2(u,v));
                }
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int a = y*(columns+1)+x;
                    triangles.AddRange(new[] {a,a+columns+1,a+1,a+1,a+columns+1,a+columns+2});
                }
            Mesh mesh = new Mesh {name = "Convex glass"};
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return SaveMesh(mesh);
        }

        private static Mesh Ring(string name, Vector4[] profiles)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            int count = Contour(profiles[0]).Length;
            foreach (Vector4 profile in profiles)
                vertices.AddRange(Contour(profile));
            // Shared vertices round the lighting over the reveal instead of making flat bright strips.
            for (int ring = 0; ring < profiles.Length - 1; ring++)
            {
                for (int i = 0; i < count; i++)
                {
                    int a = ring*count+i, b = ring*count+(i+1)%count;
                    triangles.AddRange(new[] {a,b,b+count, a,b+count,a+count});
                }
            }
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return SaveMesh(mesh);
        }

        private static Mesh SaveMesh(Mesh mesh)
        {
            string path = Folder + "/" + mesh.name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
                AssetDatabase.CreateAsset(mesh, path);
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                mesh = existing;
            }
            return mesh;
        }

        private static Vector3[] Contour(Vector4 profile)
        {
            var points = new List<Vector3>();
            Vector2[] signs = {new Vector2(1,1),new Vector2(-1,1),new Vector2(-1,-1),new Vector2(1,-1)};
            for (int corner = 0; corner < 4; corner++)
                for (int step = 0; step <= 12; step++)
                {
                    float angle = (corner * 90f + step * 7.5f) * Mathf.Deg2Rad;
                    Vector2 center = Vector2.Scale(signs[corner], new Vector2(profile.x-profile.w,profile.y-profile.w));
                    points.Add(new Vector3(center.x + Mathf.Cos(angle)*profile.w, center.y + Mathf.Sin(angle)*profile.w, profile.z));
                }
            return points.ToArray();
        }
    }
}
