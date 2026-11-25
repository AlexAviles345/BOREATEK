using UnityEngine;
using UnityEngine.UI;

namespace Boreatek.UI
{
    internal enum BoreatekGlyph { Aim, Thermos, Cookie, Candy, Garland, Beacon, Firecracker, Automaton }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BoreatekUiGlyph : MaskableGraphic
    {
        // A pair of integers is a vertex on a 16 by 16 design grid.
        private static readonly int[][][] Strokes =
        {
            new[] { new[] { 8, 1, 8, 5 }, new[] { 8, 11, 8, 15 }, new[] { 1, 8, 5, 8 }, new[] { 11, 8, 15, 8 } },
            new[] { new[] { 5, 14, 11, 14, 11, 11, 12, 10, 12, 2, 4, 2, 4, 10, 5, 11, 5, 14 }, new[] { 4, 9, 12, 9 }, new[] { 7, 5, 9, 5 } },
            new[] { new[] { 5, 14, 10, 14, 14, 11, 14, 6, 11, 2, 6, 2, 2, 5, 2, 10, 5, 14 }, new[] { 6, 10, 7, 10 }, new[] { 9, 7, 10, 7 }, new[] { 5, 5, 6, 5 } },
            new[] { new[] { 7, 2, 7, 11, 8, 13, 10, 14, 12, 13, 13, 11, 13, 9 }, new[] { 5, 2, 9, 2 }, new[] { 5, 6, 9, 8 }, new[] { 5, 10, 9, 12 } },
            new[] { new[] { 1, 13, 4, 11, 8, 10, 12, 11, 15, 13 }, new[] { 4, 11, 4, 8, 2, 6, 4, 4, 6, 6, 4, 8 }, new[] { 12, 11, 12, 8, 10, 6, 12, 4, 14, 6, 12, 8 } },
            new[] { new[] { 8, 16, 8, 14 }, new[] { 2, 13, 4, 11 }, new[] { 14, 13, 12, 11 }, new[] { 3, 2, 13, 2, 13, 4, 11, 4, 11, 9, 9, 11, 7, 11, 5, 9, 5, 4, 3, 4, 3, 2 }, new[] { 5, 4, 11, 4 } },
            new[] { new[] { 4, 2, 10, 2, 10, 10, 4, 10, 4, 2 }, new[] { 7, 10, 7, 13, 10, 14, 12, 13 }, new[] { 13, 15, 14, 16 }, new[] { 14, 11, 15, 10 }, new[] { 4, 6, 10, 6 } },
            new[] { new[] { 3, 11, 13, 11, 13, 4, 3, 4, 3, 11 }, new[] { 8, 11, 8, 15, 11, 15 }, new[] { 5, 8, 6, 8 }, new[] { 10, 8, 11, 8 }, new[] { 1, 9, 1, 6 }, new[] { 15, 9, 15, 6 }, new[] { 5, 2, 11, 2 } }
        };

        private BoreatekGlyph _glyph;

        internal void SetGlyph(BoreatekGlyph glyph)
        {
            _glyph = glyph;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = GetPixelAdjustedRect();
            float size = Mathf.Min(bounds.width, bounds.height);
            if (size <= 0f)
            {
                return;
            }

            float unit = size / 16f;
            Vector2 origin = bounds.center - Vector2.one * size * 0.5f;
            foreach (int[] path in Strokes[(int)_glyph])
            {
                for (int i = 2; i < path.Length; i += 2)
                {
                    Vector2 from = origin + new Vector2(path[i - 2], path[i - 1]) * unit;
                    Vector2 to = origin + new Vector2(path[i], path[i + 1]) * unit;
                    Vector2 direction = (to - from).normalized;
                    Vector2 normal = Vector2.Perpendicular(direction) * (0.66f * unit);
                    Vector2 cap = direction * (0.35f * unit);
                    int index = mesh.currentVertCount;
                    mesh.AddVert(from - cap - normal, color, Vector2.zero);
                    mesh.AddVert(from - cap + normal, color, Vector2.zero);
                    mesh.AddVert(to + cap + normal, color, Vector2.zero);
                    mesh.AddVert(to + cap - normal, color, Vector2.zero);
                    mesh.AddTriangle(index, index + 1, index + 2);
                    mesh.AddTriangle(index, index + 2, index + 3);
                }
            }
        }
    }
}
