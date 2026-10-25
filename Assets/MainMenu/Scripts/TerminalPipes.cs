using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Original pipe geometry. Each segment follows one axis of a small 3D grid.
    [RequireComponent(typeof(CanvasRenderer))]
    public class TerminalPipes : MaskableGraphic
    {
        protected TerminalPipes()
        {
            useLegacyMeshGeneration = false;
        }
        private struct Segment
        {
            public Vector3 a, b; public Color color;
        }
        private readonly List<Segment> segments = new List<Segment>();
        private Vector3 head;
        private int lastAxis;
        private float nextStep;
        public void ResetPipes()
        {
            segments.Clear();
            head = Vector3.zero;
            nextStep = 0;
            SetVerticesDirty();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            canvasRenderer.SetColor(Color.white);
        }
        private void Update()
        {
            if (Time.unscaledTime < nextStep)
                return;
            nextStep = Time.unscaledTime + (GamePreferences.ReducedMotion ? .7f : .16f);
            if (segments.Count > 200)
                ResetPipes();
            int axis = (lastAxis + Random.Range(1, 3)) % 3;
            Vector3 end = head;
            end[axis] += Random.value < .5f ? -1 : 1;
            if (Mathf.Abs(end[axis]) > 5)
                end[axis] = head[axis] - Mathf.Sign(end[axis]);
            segments.Add(new Segment { a = head, b = end, color = Color.Lerp(new Color(.03f, .15f, .6f), new Color(.25f, .85f, 1), Random.value) });
            head = end;
            lastAxis = axis;
            SetVerticesDirty();
        }
        private Vector2 Project(Vector3 p)
        {
            float size = Mathf.Min(rectTransform.rect.width / 18, rectTransform.rect.height / 16);
            return new Vector2(p.x + p.z * .55f, p.y + p.z * .34f) * size;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!enabled)
                return;
            foreach (var pipe in segments)
            {
                Vector2 a = Project(pipe.a), b = Project(pipe.b);
                Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized;
                float width = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .012f;
                for (int band = 0; band < 5; band++)
                {
                    float lo = (band / 5f - .5f) * width, hi = ((band + 1) / 5f - .5f) * width;
                    Color shade = pipe.color * Mathf.Lerp(.25f, 1, Mathf.Sin((band + .5f) / 5 * Mathf.PI));
                    shade.a = 1;
                    int index = mesh.currentVertCount;
                    mesh.AddVert(a + side * lo, shade, Vector2.zero);
                    mesh.AddVert(a + side * hi, shade, Vector2.zero);
                    mesh.AddVert(b + side * hi, shade, Vector2.zero);
                    mesh.AddVert(b + side * lo, shade, Vector2.zero);
                    mesh.AddTriangle(index, index + 1, index + 2);
                    mesh.AddTriangle(index, index + 2, index + 3);
                }
            }
        }
    }
}
