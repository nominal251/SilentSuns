using UnityEngine;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // Simple screen drawings, kept sharp at the monitor's own resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public class MasterComputerGraphic : MaskableGraphic
    {
        public enum Drawing { Ship, Caution, Tape, Grid, Disk, Report, Clear }
        public Drawing drawing;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (rectTransform.rect.width <= 0 || rectTransform.rect.height <= 0) return;
            if (drawing == Drawing.Caution)
            {
                Triangle(mesh, new Vector2(.5f, .98f), new Vector2(.02f, .02f), new Vector2(.98f, .02f), color);
                Box(mesh, .46f, .32f, .08f, .36f, TerminalUI.Dark);
                Box(mesh, .46f, .16f, .08f, .08f, TerminalUI.Dark);
            }
            else if (drawing == Drawing.Tape)
            {
                float offset = GamePreferences.ReducedMotion ? 0 : Mathf.Repeat(Time.unscaledTime * .025f, .1f);
                for (float x = -.15f + offset; x < 1.1f; x += .1f)
                {
                    Vector2 a = new Vector2(x, 0), b = new Vector2(x + .04f, 0), c = new Vector2(x + .08f, 1), d = new Vector2(x + .04f, 1);
                    Triangle(mesh, a, b, c, color); Triangle(mesh, a, c, d, color);
                }
            }
            else if (drawing == Drawing.Grid)
            {
                for (float x = .05f; x < 1; x += .1f)
                    for (float y = .08f; y < 1; y += .16f)
                        Box(mesh, x, y, .002f, .005f, color);
                // Registration corners keep the diagram contained without another full border.
                foreach (float x in new[] { 0f, .96f })
                    foreach (float y in new[] { 0f, .98f })
                        Box(mesh, x, y, .04f, .01f, color);
                Box(mesh, 0, 0, .003f, .12f, color); Box(mesh, .997f, 0, .003f, .12f, color);
                Box(mesh, 0, .88f, .003f, .12f, color); Box(mesh, .997f, .88f, .003f, .12f, color);
            }
            else if (drawing == Drawing.Disk)
            {
                Outline(mesh, .10f, .06f, .80f, .88f);
                Outline(mesh, .28f, .57f, .46f, .37f);
                Box(mesh, .59f, .63f, .09f, .25f, color);
                Outline(mesh, .22f, .06f, .56f, .36f);
                Box(mesh, .30f, .28f, .40f, .014f, color);
                Box(mesh, .30f, .18f, .27f, .014f, color);
            }
            else if (drawing == Drawing.Report)
            {
                Outline(mesh, .16f, .05f, .68f, .90f);
                Outline(mesh, .25f, .64f, .18f, .20f);
                Box(mesh, .51f, .79f, .23f, .018f, color);
                Box(mesh, .51f, .68f, .17f, .018f, color);
                for (float y = .22f; y < .55f; y += .14f) Box(mesh, .26f, y, .48f, .018f, color);
            }
            else if (drawing == Drawing.Clear)
            {
                // Fit a real square, with strokes thick enough to survive the CRT sampling.
                var rect = rectTransform.rect;
                float size = Mathf.Min(rect.width, rect.height);
                var scale = new Vector2(size / rect.width, size / rect.height);
                var origin = (Vector2.one - scale) * .5f;
                Outline(mesh, origin.x + .12f * scale.x, origin.y + .12f * scale.y, .76f * scale.x, .76f * scale.y);
                Check(mesh, origin, scale, Mathf.Max(2, size * .035f) / size);
            }
            else
            {
                // Whole-ship outline only. The game does not expose damage by module yet.
                Outline(mesh, .10f, .42f, .74f, .16f);
                Outline(mesh, .14f, .35f, .18f, .30f);
                Outline(mesh, .35f, .36f, .13f, .28f);
                Outline(mesh, .52f, .37f, .11f, .26f);
                Outline(mesh, .70f, .34f, .08f, .32f);
                Outline(mesh, .82f, .30f, .10f, .40f);
                Outline(mesh, .55f, .05f, .07f, .28f);
                Outline(mesh, .55f, .67f, .07f, .28f);
                Box(mesh, .58f, .05f, .008f, .90f, color);
                Box(mesh, .04f, .49f, .10f, .02f, color);
            }
        }
        private void Check(VertexHelper mesh, Vector2 origin, Vector2 scale, float width)
        {
            var a = new Vector2(.27f, .48f);
            var b = new Vector2(.43f, .31f);
            var c = new Vector2(.75f, .72f);
            Vector2 first = (b - a).normalized, second = (c - b).normalized;
            var n1 = new Vector2(-first.y, first.x);
            var n2 = new Vector2(-second.y, second.x);
            Vector2 join = (n1 + n2).normalized;
            join *= width * .5f / Vector2.Dot(join, n1);
            // Both arms share the same corner, rather than overlapping two capped lines.
            Vector2[] points = { a - n1 * width * .5f, a + n1 * width * .5f,
                b - join, b + join, c - n2 * width * .5f, c + n2 * width * .5f };
            int start = mesh.currentVertCount;
            var rect = rectTransform.rect;
            foreach (var point in points)
            {
                Vector2 fitted = origin + Vector2.Scale(point, scale);
                mesh.AddVert(new Vector3(rect.xMin + fitted.x * rect.width, rect.yMin + fitted.y * rect.height), color, Vector2.zero);
            }
            mesh.AddTriangle(start, start + 2, start + 3); mesh.AddTriangle(start, start + 3, start + 1);
            mesh.AddTriangle(start + 2, start + 4, start + 5); mesh.AddTriangle(start + 2, start + 5, start + 3);
        }
        private void Outline(VertexHelper mesh, float x, float y, float w, float h)
        {
            var rect = rectTransform.rect;
            float stroke = Mathf.Max(1.6f, Mathf.Min(rect.width, rect.height) * .008f);
            float sx = stroke / Mathf.Max(1, rect.width), sy = stroke / Mathf.Max(1, rect.height);
            Box(mesh, x, y, w, sy, color); Box(mesh, x, y + h - sy, w, sy, color);
            Box(mesh, x, y + sy, sx, h - sy * 2, color); Box(mesh, x + w - sx, y + sy, sx, h - sy * 2, color);
        }
        private void Box(VertexHelper mesh, float x, float y, float w, float h, Color tint)
        {
            var a = new Vector2(x, y); var b = new Vector2(x + w, y); var c = new Vector2(x + w, y + h); var d = new Vector2(x, y + h);
            Triangle(mesh, a, b, c, tint); Triangle(mesh, a, c, d, tint);
        }
        private void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int start = mesh.currentVertCount;
            var rect = rectTransform.rect;
            foreach (Vector2 point in new[] { a, b, c })
                mesh.AddVert(new Vector3(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
        private void Update() { if (drawing == Drawing.Tape && !GamePreferences.ReducedMotion) SetVerticesDirty(); }
    }
}
