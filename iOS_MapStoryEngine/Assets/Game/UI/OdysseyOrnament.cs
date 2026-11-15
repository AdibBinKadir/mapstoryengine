using UnityEngine;
using UnityEngine.UI;

namespace Odyssey
{
    internal sealed class OdysseyOrnament : MaskableGraphic
    {
        internal enum Design { Meander, Oracle, Clues, Rules, Locations }
        private Design design;

        internal static void Draw(RectTransform rect, Design design, Color color)
        {
            var graphic = rect.gameObject.AddComponent<OdysseyOrnament>();
            graphic.design = design;
            graphic.color = color;
            graphic.raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = GetPixelAdjustedRect();
            if (design == Design.Meander)
            {
                int count = Mathf.FloorToInt(bounds.width / 22);
                float x = bounds.center.x - count * 11;
                for (int i = 0; i < count; i++, x += 22)
                {
                    float y = bounds.center.y - 3;
                    Path(mesh, new Vector2(x, y), 1, 1.6f, 0, 0, 0, 8, 16, 8, 16, 0, 5, 0,
                        5, 4, 11, 4, 11, 2);
                }
                return;
            }

            float scale = Mathf.Min(bounds.width, bounds.height) / 24;
            Vector2 origin = bounds.center - Vector2.one * (12 * scale);
            switch (design)
            {
                case Design.Oracle:
                    Path(mesh, origin, scale, 1.4f, 1, 12, 6, 17, 12, 19, 18, 17, 23, 12,
                        18, 7, 12, 5, 6, 7, 1, 12);
                    Path(mesh, origin, scale, 1.6f, 12, 8, 16, 12, 12, 16, 8, 12, 12, 8);
                    break;
                case Design.Clues:
                    Path(mesh, origin, scale, 1.35f, 5, 4, 5, 20, 19, 20, 19, 8, 15, 4, 5, 4,
                        15, 4, 15, 8, 19, 8);
                    Path(mesh, origin, scale, 1.35f, 8, 14, 16, 14);
                    Path(mesh, origin, scale, 1.35f, 8, 10, 13, 10);
                    break;
                case Design.Rules:
                    Path(mesh, origin, scale, 1.45f, 2, 16, 12, 22, 22, 16, 2, 16);
                    Path(mesh, origin, scale, 1.45f, 5, 14, 5, 5, 19, 5, 19, 14);
                    Path(mesh, origin, scale, 1.45f, 12, 14, 12, 5);
                    Path(mesh, origin, scale, 1.45f, 2, 2, 22, 2);
                    break;
                case Design.Locations:
                    Path(mesh, origin, scale, 1.45f, 12, 2, 4, 13, 5, 18, 8, 21, 16, 21, 19, 18,
                        20, 13, 12, 2);
                    Path(mesh, origin, scale, 1.45f, 12, 18, 15, 15, 12, 12, 9, 15, 12, 18);
                    break;
            }
        }

        private void Path(VertexHelper mesh, Vector2 origin, float scale, float thickness, params int[] points)
        {
            for (int i = 2; i < points.Length; i += 2)
            {
                var a = origin + new Vector2(points[i - 2], points[i - 1]) * scale;
                var b = origin + new Vector2(points[i], points[i + 1]) * scale;
                Vector2 delta = (b - a).normalized;
                Vector2 normal = new Vector2(-delta.y, delta.x) * (thickness * 0.5f * scale);
                int index = mesh.currentVertCount;
                mesh.AddVert(a - normal, color, Vector2.zero);
                mesh.AddVert(a + normal, color, Vector2.zero);
                mesh.AddVert(b + normal, color, Vector2.zero);
                mesh.AddVert(b - normal, color, Vector2.zero);
                mesh.AddTriangle(index, index + 1, index + 2);
                mesh.AddTriangle(index, index + 2, index + 3);
            }
        }
    }
}
