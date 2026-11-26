using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private const float MineCoverEnd = 26f;
        private const float MineVaultHeightScale = 0.7f;

        internal static bool IsMineEntranceCutout(float x, float z) =>
            x >= -4f && x < 4f && z >= 14f && z < MineCoverEnd;

        private void BuildCoveredMineEntrance(Transform tunnel, Vector3 rampStart, Quaternion rampRotation)
        {
            var m = new FoundryMaterials(_landscape);
            var cover = new Batch();
            var portal = new Batch();
            var fittings = new Batch();
            Material frost = _landscape.Finish(new Color32(171, 201, 219, 255));
            Material exposedRock = _landscape.Finish(new Color32(66, 84, 101, 255));
            var span = new[] { -5.8f, -4.12f, -3.42f, -2.55f, -1.5f, 0f, 1.5f, 2.55f, 3.42f, 4.12f, 5.8f };
            var crown = new[] { 0.025f, 0.49f, 2.23f, 3.22f, 3.62f, 3.85f, 3.67f, 3.19f, 2.24f, 0.55f, 0.025f };
            var opening = new[] { 0.01f, 0f, 0.14f, 2.43f, 2.72f, 2.89f, 2.72f, 2.43f, 0.14f, 0f, 0.01f };
            var rows = new[] { 15.1f, 17.4f, 20.15f, 23.45f, MineCoverEnd };
            var surface = new Vector3[rows.Length, span.Length];
            for (int row = 0; row < rows.Length; row++)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    float z = rows[row];
                    Vector3 snowline = SurfacePoint(span[i], z);
                    float aboveSnow = row == rows.Length - 1 ? 0.004f :
                        Mathf.Max(0.015f, crown[i] - (z - rows[0]) / 3f);
                    surface[row, i] = snowline + Vector3.up * aboveSnow;
                }
            }
            for (int row = 1; row < rows.Length; row++)
            {
                for (int i = 1; i < span.Length; i++)
                {
                    cover.Triangle(surface[row - 1, i - 1], surface[row, i - 1], surface[row - 1, i],
                        (i + row) % 3 == 0 ? frost : m.Snow);
                    cover.Triangle(surface[row - 1, i], surface[row, i - 1], surface[row, i],
                        (i + row) % 3 == 1 ? frost : m.Snow);
                }
            }
            for (int i = 1; i < span.Length; i++)
            {
                Vector3 a = new Vector3(span[i - 1], opening[i - 1], rows[0]);
                Vector3 b = new Vector3(span[i], opening[i], rows[0]);
                portal.Quad(a, surface[0, i - 1], surface[0, i], b, exposedRock);
                if (i >= 3 && i <= 8)
                {
                    Beam(fittings, a + new Vector3(0f, 0.15f, -0.095f), b + new Vector3(0f, 0.15f, -0.095f),
                        0.23f, i % 2 == 0 ? m.Iron : m.Timber);
                    Vector3 innerA = RampVaultEdge(a.x, rampStart, rampRotation);
                    Vector3 innerB = RampVaultEdge(b.x, rampStart, rampRotation);
                    portal.Quad(a, b, innerB, innerA, exposedRock);
                }
            }
            portal.Quad(new Vector3(-4.05f, 0.075f, 13.74f), new Vector3(-4.05f, 0.264f, 15.14f),
                new Vector3(4.05f, 0.264f, 15.14f), new Vector3(4.05f, 0.075f, 13.74f), m.Stone);
            for (int side = -1; side <= 1; side += 2)
            {
                var lamp = new Vector3(side * 3.48f, 1.47f, 14.79f);
                AddFoundryLantern(fittings, lamp, 0.92f, m);
                AddFoundryLight(tunnel, lamp + Vector3.back * 0.25f, 1.5f, 4.7f);
            }
            fittings.Box(new Vector3(0f, 3.37f, 14.96f), new Vector3(2.36f, 0.30f, 0.18f), m.Iron);
            AddFoundrySign(tunnel, "ACCESO MINERO", new Vector3(0f, 3.37f, 14.84f));
            _landscape.Place(tunnel, "Snow_Over_Ventilation_Tunnel", cover, collision: true);
            _landscape.Place(tunnel, "Mine_Entrance_Arch_And_Approach", portal, collision: true);
            _landscape.Place(tunnel, "Mine_Entrance_Beams_And_Lanterns", fittings);
        }

        private static Vector3 RampVaultEdge(float x, Vector3 start, Quaternion rotation)
        {
            for (int i = 2; i < TunnelProfile.Length - 1; i++)
            {
                Vector2 a = TunnelProfile[i - 1], b = TunnelProfile[i];
                if (x > b.x) continue;
                float height = Mathf.Lerp(a.y, b.y, Mathf.InverseLerp(a.x, b.x, x)) * MineVaultHeightScale;
                return start + rotation * new Vector3(x, height, 0f);
            }
            return start + rotation * new Vector3(x, TunnelProfile[^2].y * MineVaultHeightScale, 0f);
        }
    }
}
