using UnityEngine;
using UnityEngine.UI;

namespace SilentSuns.Menu
{
    // The picture and the physical glass use the same shallow convex surface.
    public class TerminalGlassGraphic : RawImage
    {
        public const float Bow = 0.18f;
        public const int Columns = 48;
        public const int Rows = 28;

        public static float Depth(float x, float y, float bow)
        {
            return -bow * (1f - x * x) * (1f - y * y);
        }

        public float LocalDepth(Vector2 point)
        {
            Rect rect = rectTransform.rect;
            return Depth((point.x - rect.center.x) / (rect.width * .5f),
                (point.y - rect.center.y) / (rect.height * .5f), Bow / transform.lossyScale.z);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            for (int y = 0; y <= Rows; y++)
                for (int x = 0; x <= Columns; x++)
                {
                    Vector2 uv = new Vector2((float)x / Columns, (float)y / Rows);
                    Vector3 point = new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, uv.x), Mathf.Lerp(rect.yMin, rect.yMax, uv.y));
                    point.z = LocalDepth(point);
                    mesh.AddVert(point, color, uv);
                }
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                {
                    int a = y * (Columns + 1) + x;
                    mesh.AddTriangle(a, a + Columns + 1, a + 1);
                    mesh.AddTriangle(a + 1, a + Columns + 1, a + Columns + 2);
                }
        }

        public bool ScreenToSurface(Camera camera, Vector2 screenPoint, out Vector2 point)
        {
            Ray ray = camera.ScreenPointToRay(screenPoint);
            Vector3 origin = rectTransform.InverseTransformPoint(ray.origin);
            Vector3 direction = rectTransform.InverseTransformVector(ray.direction);
            point = Vector2.zero;
            if (Mathf.Abs(direction.z) < .0001f) return false;
            float distance = -origin.z / direction.z;
            // The shallow surface converges quickly; pointer hits include the actual mesh depth.
            for (int i = 0; i < 8; i++)
            {
                Vector3 hit = origin + direction * distance;
                distance = (LocalDepth(hit) - origin.z) / direction.z;
            }
            point = origin + direction * distance;
            return distance >= 0f && rectTransform.rect.Contains(point);
        }
    }
}
