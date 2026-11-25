using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private static readonly Vector2[] TunnelProfile =
        {
            new Vector2(-3.43f, 0.25f), new Vector2(-3.43f, 2.58f), new Vector2(-2.81f, 3.82f),
            new Vector2(-1.62f, 4.56f), new Vector2(0f, 4.83f), new Vector2(1.62f, 4.56f),
            new Vector2(2.81f, 3.82f), new Vector2(3.43f, 2.58f), new Vector2(3.43f, 0.25f)
        };

        private static readonly Vector2[] CavernProfile =
        {
            new Vector2(-0.959f, 0.0f), new Vector2(-0.93f, 0.24f), new Vector2(-0.88f, 0.56f),
            new Vector2(-0.75f, 0.81f), new Vector2(-0.45f, 0.96f), new Vector2(-0.14f, 0.96f),
            new Vector2(0.18f, 0.98f), new Vector2(0.51f, 0.91f), new Vector2(0.77f, 0.75f),
            new Vector2(0.91f, 0.53f), new Vector2(0.93f, 0.21f), new Vector2(0.959f, 0.0f)
        };

        private void DressTunnel(Transform tunnel, Vector3 start, Quaternion rotation, float length, Color wallColor)
        {
            var liner = new Batch();
            Material stone = _landscape.Finish(wallColor * 0.73f);
            Material rockBand = _landscape.Finish(wallColor * 0.87f);
            Material rib = _landscape.Finish(Steel, smoothness: 0.42f);
            Material copper = _landscape.Finish(Brass, smoothness: 0.55f);
            Material lamp = _landscape.Finish(Furnace, 1.8f);

            for (int panel = 0; panel < TunnelProfile.Length - 1; panel++)
            {
                Vector2 a = TunnelProfile[panel], b = TunnelProfile[panel + 1];
                liner.Quad(new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f),
                    new Vector3(b.x, b.y, length), new Vector3(a.x, a.y, length),
                    panel % 2 == 0 ? stone : rockBand);
            }

            for (float z = 0.3f; z < length; z += 3.9f)
            {
                for (int segment = 0; segment < TunnelProfile.Length - 1; segment++)
                {
                    Vector2 a = TunnelProfile[segment] * 0.98f, b = TunnelProfile[segment + 1] * 0.98f;
                    liner.Frustum(new Vector3(a.x, a.y, z), new Vector3(b.x, b.y, z),
                        0.105f, 0.105f, 6, rib);
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    liner.Box(new Vector3(side * 3.27f, 2.77f, z), new Vector3(0.24f, 0.19f, 0.7f), lamp,
                        Quaternion.Euler(0f, 0f, side * 20f));
                    liner.Box(new Vector3(side * 3.12f, 0.83f, z), new Vector3(0.24f, 0.48f, 0.13f), rib);
                }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int line = 0; line < 2; line++)
                {
                    Vector3 end = new Vector3(side * (3.14f - 0.065f * line), 0.75f + line * 0.31f, 0.18f);
                    liner.Frustum(end, end + Vector3.forward * (length - 0.36f), 0.09f, 0.09f, 7, copper);
                }
            }
            Transform shell = _landscape.Place(tunnel, "RivetedVault_PipesAndRouteLights", liner).transform;
            shell.localPosition = start;
            shell.localRotation = rotation;
        }

        private void CloseChamberEnds(Transform chamber, Vector3 size, Color rockColor, bool hasNorthExit)
        {
            const float opening = 7.8f;
            float wing = (size.x - opening) * 0.5f;
            for (int end = -1; end <= 1; end += 2)
            {
                float z = end * (size.z * 0.5f - 0.23f);
                if (end == 1 && !hasNorthExit)
                {
                    CreateBox(chamber, "Chamber_BackWall", new Vector3(0f, size.y * 0.5f, z),
                        new Vector3(size.x, size.y, 0.7f), rockColor * 0.76f);
                    continue;
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    CreateBox(chamber, $"Passage_Wing_{end}_{side}", new Vector3(side * (opening + wing) * 0.5f, size.y * 0.5f, z),
                        new Vector3(wing, size.y, 0.7f), rockColor * 0.82f);
                }
                float lintelHeight = size.y - 5.4f;
                CreateBox(chamber, $"Passage_Lintel_{end}", new Vector3(0f, size.y - lintelHeight * 0.5f, z),
                    new Vector3(opening, lintelHeight, 0.7f), rockColor * 0.79f);
            }
        }

        private void DressCavern(Transform chamber, Vector3 size, int stratum, Color rock, Color glow)
        {
            var walls = new Batch();
            Material shale = _landscape.Finish(rock * 0.63f);
            Material edge = _landscape.Finish(Color.Lerp(rock, glow, 0.3f), smoothness: 0.4f);
            Material shard = _landscape.Finish(glow, stratum == 3 ? 0.14f : 0.55f, 0.64f);
            Material trail = _landscape.Finish(new Color32(243, 122, 53, 255), 1.1f);
            var art = new System.Random(_worldSeed ^ (1009 * stratum));
            AddCavernEnvelope(walls, size, stratum, rock, glow);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 13; i++)
                {
                    float z = -size.z * 0.5f + 1.5f + i * (size.z - 3f) / 12f;
                    float x = side * (size.x * 0.5f - 0.46f);
                    float spread = 0.7f + (float)art.NextDouble() * 0.7f;
                    var ceiling = new Vector3(x - side * (0.8f + (float)art.NextDouble() * 4.8f), size.y - 0.22f, z);
                    var floor = new Vector3(x, 0.08f, z);
                    walls.Frustum(floor, floor + new Vector3(-side * 0.33f, 1.9f + spread, 0.38f),
                        spread, spread * 0.48f, 6, shale);
                    walls.Frustum(ceiling, ceiling - new Vector3(side * 0.6f, 1.65f + spread, 0.27f),
                        spread * 0.89f, 0f, 6, i % 3 == 0 ? shard : edge);
                    if (i % 2 == 0 && stratum < 3)
                    {
                        Vector3 crystal = floor + new Vector3(-side * 0.8f, 0f, 0.42f);
                        float height = stratum == 2 ? 2.6f + spread : 0.8f + spread;
                        AddCrystal(walls, crystal, crystal + new Vector3(-side * 0.72f, height, 0.2f),
                            stratum == 2 ? 0.82f : 0.43f, edge, shard);
                    }
                }
                for (int i = 0; i < 7; i++)
                {
                    float z = -size.z * 0.5f + 2f + i * 3.9f;
                    walls.Box(new Vector3(side * 3.4f, 0.045f, z), new Vector3(0.28f, 0.08f, 0.8f), trail);
                }
            }
            _landscape.Place(chamber, "Cavern_FacetedWallsAndRouteMarkers", walls);
            if (stratum == 2) DressGeodeGate(chamber, size);
            if (stratum == 3) DressGeothermalFissure(chamber, size);
        }

        private void AddCavernEnvelope(Batch shell, Vector3 size, int stratum, Color rock, Color glow)
        {
            Color tint = stratum == 3 ? new Color(0.23f, 0.20f, 0.21f) : glow;
            Material[] facets =
            {
                _landscape.Finish(rock * 0.78f),
                _landscape.Finish(Color.Lerp(rock, tint, 0.16f)),
                _landscape.Finish(Color.Lerp(rock, tint, 0.37f), stratum == 2 ? 0.1f : 0f, 0.38f),
                _landscape.Finish(Color.Lerp(rock, tint, 0.57f), stratum == 2 ? 0.29f : 0.02f, 0.42f)
            };
            const int slices = 11;
            var points = new Vector3[slices + 1, CavernProfile.Length];
            for (int row = 0; row <= slices; row++)
            {
                float z = Mathf.Lerp(-size.z * 0.5f, size.z * 0.5f, row / (float)slices);
                for (int column = 0; column < CavernProfile.Length; column++)
                {
                    Vector2 rim = CavernProfile[column];
                    float uneven = Mathf.PerlinNoise(row * 0.66f + stratum * 9f, column * 0.71f + 19f) - 0.5f;
                    float lift = rim.y > 0.2f ? uneven * 1.20f : 0f;
                    float x = rim.x * size.x * 0.5f + lift * 0.85f;
                    float y = rim.y * size.y + lift;
                    float depth = z + (row > 0 && row < slices ? uneven * 0.75f : 0f);
                    points[row, column] = new Vector3(x, y, depth);
                }
            }
            for (int row = 0; row < slices; row++)
            {
                for (int column = 0; column < CavernProfile.Length - 1; column++)
                {
                    Vector3 a = points[row, column], b = points[row, column + 1];
                    Vector3 c = points[row + 1, column + 1], d = points[row + 1, column];
                    int shade = CaveFacetShade(row, column, stratum);
                    shell.Triangle(a, b, c, facets[shade]);
                    shell.Triangle(a, c, d, facets[CaveFacetShade(row + 13, column + 3, stratum)]);
                }
            }
        }

        private static int CaveFacetShade(int row, int column, int stratum)
        {
            float shade = Mathf.PerlinNoise(row * 0.61f + 55f, column * 0.41f + 13f * stratum);
            return Mathf.Clamp(Mathf.FloorToInt(shade * 4.5f), 0, 3);
        }

        private void DressGeodeGate(Transform chamber, Vector3 size)
        {
            var arch = new Batch();
            Material outerIce = _landscape.Finish(new Color32(58, 141, 187, 255), 0.35f, 0.48f);
            Material coreIce = _landscape.Finish(new Color32(58, 183, 229, 255), 0.9f, 0.48f);
            Material frostedTip = _landscape.Finish(new Color32(147, 220, 247, 255), 0.92f, 0.59f);
            const int sectors = 12;
            float z = size.z * 0.5f - 0.8f;
            for (int i = 0; i < sectors; i++)
            {
                Vector3 a = GeodeRim(i * Mathf.PI / sectors, z);
                Vector3 b = GeodeRim((i + 1) * Mathf.PI / sectors, z);
                arch.Frustum(a, b, 0.58f, 0.53f, 6, i % 3 == 0 ? outerIce : coreIce);
                if (i % 2 == 0)
                {
                    float angle = (i + 0.4f) * Mathf.PI / sectors;
                    Vector3 root = Vector3.Lerp(a, b, 0.4f);
                    var edge = new Vector3(Mathf.Cos(angle) * 0.96f, Mathf.Sin(angle) * 1.11f, -0.37f);
                    AddCrystal(arch, root, root + edge, 0.34f, coreIce, frostedTip);
                }
            }
            _landscape.Place(chamber, "Geode_IceFractureGate", arch);
        }

        private static Vector3 GeodeRim(float angle, float z)
        {
            return new Vector3(Mathf.Cos(angle) * 4.72f, 0.54f + Mathf.Sin(angle) * 5.58f,
                z + Mathf.Sin(angle * 5f) * 0.21f);
        }

        private void DressGeothermalFissure(Transform chamber, Vector3 size)
        {
            var fault = new Batch();
            Material crust = _landscape.Finish(new Color32(88, 33, 27, 255), 0.25f);
            Material ember = _landscape.Finish(new Color32(224, 68, 15, 255), 1.8f);
            Material heat = _landscape.Finish(new Color32(255, 146, 36, 255), 2.65f);
            float wallZ = size.z * 0.5f - 0.64f;
            const int strips = 14;
            Vector3 bottom = new Vector3(0f, 0.08f, wallZ);
            for (int i = 0; i < strips; i++)
            {
                float t = i / (float)strips, next = (i + 1f) / strips;
                Vector3 a = bottom + new Vector3(Mathf.Sin(t * 17f) * 0.72f, t * (size.y - 0.55f), 0f);
                Vector3 b = bottom + new Vector3(Mathf.Sin(next * 17f) * 0.72f, next * (size.y - 0.55f), 0f);
                AddFissureStrip(fault, a, b, 1.19f, crust);
                AddFissureStrip(fault, a - Vector3.forward * 0.035f, b - Vector3.forward * 0.035f, 0.68f, ember);
                AddFissureStrip(fault, a - Vector3.forward * 0.06f, b - Vector3.forward * 0.06f, 0.27f, heat);
            }
            var pool = new Vector3(0f, 0.035f, size.z * 0.5f - 2.15f);
            fault.Frustum(pool, pool + Vector3.up * 0.019f, 2.5f, 2.48f, 11, crust);
            fault.Frustum(pool + Vector3.up * 0.02f, pool + Vector3.up * 0.032f, 1.73f, 1.66f, 9, ember);
            fault.Frustum(pool + Vector3.up * 0.034f, pool + Vector3.up * 0.041f, 0.92f, 0.86f, 7, heat);
            _landscape.Place(chamber, "Abyss_FestiveMagmaFissure", fault);
        }

        private static void AddFissureStrip(Batch batch, Vector3 a, Vector3 b, float width, Material material)
        {
            Vector3 side = Vector3.right * width * 0.5f;
            batch.Quad(a - side, b - side, b + side, a + side, material);
        }

        private void DressMineralVein(Transform vein, Color color, float emission)
        {
            var mineral = new Batch();
            Material bedrock = _landscape.Finish(new Color32(46, 57, 68, 255));
            Material body = _landscape.Finish(color, emission * 0.55f, 0.43f);
            Material tip = _landscape.Finish(Color.Lerp(color, Frost, 0.22f), emission, 0.54f);
            mineral.Frustum(Vector3.zero, new Vector3(0.04f, 0.63f, -0.08f), 0.84f, 0.59f, 7, bedrock);
            AddCrystal(mineral, new Vector3(-0.14f, 0.40f, 0.05f), new Vector3(-0.22f, 1.73f, 0.17f), 0.38f, body, tip);
            AddCrystal(mineral, new Vector3(0.19f, 0.38f, 0.02f), new Vector3(0.73f, 1.22f, -0.21f), 0.29f, body, tip);
            AddCrystal(mineral, new Vector3(-0.25f, 0.34f, -0.05f), new Vector3(-0.67f, 1.06f, -0.36f), 0.24f, body, tip);
            _landscape.Place(vein, "Mineral_FacetedShards", mineral);
        }

        private static void AddCrystal(Batch batch, Vector3 root, Vector3 tip, float width, Material core, Material point)
        {
            Vector3 shoulder = Vector3.Lerp(root, tip, 0.76f);
            batch.Frustum(root, shoulder, width * 0.86f, width, 6, core);
            batch.Frustum(shoulder, tip, width, 0f, 6, point);
        }
    }
}
