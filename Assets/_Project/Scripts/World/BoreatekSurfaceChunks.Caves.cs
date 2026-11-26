using Boreatek.Core;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private const int CaveResourceSlots = 6;
        private const int RampLength = 16;
        private const float MouthHalfWidth = 4f;
        private readonly System.Collections.Generic.Dictionary<Vector2Int, CaveSite> _caves = new();

        private readonly struct CaveSite
        {
            internal readonly Vector3 Entry, Direction, Side;
            internal readonly float Floor, Length, Width, Roof;
            internal readonly int Palette;

            internal CaveSite(Vector3 entry, Vector3 direction, float floor, float length, float width, float roof, int palette)
            {
                Entry = entry;
                Direction = direction;
                Side = Vector3.Cross(Vector3.up, direction);
                Floor = floor;
                Length = length;
                Width = width;
                Roof = roof;
                Palette = palette;
            }

            internal Vector3 At(float across, float along, float height) =>
                Entry + Side * across + Direction * along + Vector3.up * height;

            internal Vector2 Coordinates(float x, float z)
            {
                Vector3 delta = new Vector3(x - Entry.x, 0f, z - Entry.z);
                return new Vector2(Vector3.Dot(delta, Side), Vector3.Dot(delta, Direction));
            }

            internal bool OpensTerrainAt(float x, float z)
            {
                Vector2 p = Coordinates(x, z);
                return Mathf.Abs(p.x) < MouthHalfWidth && p.y > 0f && p.y < RampLength;
            }

            internal bool BlocksDressingAt(float x, float z)
            {
                Vector2 p = Coordinates(x, z);
                return Mathf.Abs(p.x) < MouthHalfWidth + 2.3f && p.y > -2.5f && p.y < RampLength + 1.5f;
            }
        }

        internal bool IsInProceduralCave(Vector3 position)
        {
            return _caves.TryGetValue(SectorAt(position), out CaveSite cave) && IsInsideCave(position, cave);
        }

        private bool IsInsideCave(Vector3 position, CaveSite cave)
        {
            Vector2 p = cave.Coordinates(position.x, position.z);
            if (p.y <= 1f || p.y > cave.Length || position.y < cave.Floor - 0.75f) return false;
            if (p.y < RampLength) return Mathf.Abs(p.x) < MouthHalfWidth &&
                position.y <= RampPoint(cave, p.x, p.y).y + 2.35f &&
                (p.y >= 2.1f || position.y <= GroundHeight(position.x, position.z) - 0.52f);
            return Mathf.Abs(p.x) < cave.Width + 0.55f && position.y <= cave.Floor + cave.Roof + 0.35f;
        }

        private CaveSite? PlanCave(Vector2Int sector, Vector3 origin)
        {
            var random = new System.Random(CellSeed(_seed ^ 0x43A91, sector.x, sector.y));
            if (random.NextDouble() > 0.24) return null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                int directionIndex = random.Next(4), transverse = 20 + random.Next(7) * VertexStep;
                Vector3 direction = directionIndex switch
                {
                    0 => Vector3.forward, 1 => Vector3.back, 2 => Vector3.right, _ => Vector3.left
                };
                int axis = directionIndex % 2 == 0 ? 16 : 48;
                Vector3 entry = origin + (directionIndex < 2 ? new Vector3(transverse, 0f, axis) : new Vector3(axis, 0f, transverse));
                float length = 28f + (float)random.NextDouble() * 6f;
                float width = 5.8f + (float)random.NextDouble() * 3.5f;
                float roof = 3.65f + (float)random.NextDouble() * 1.0f;
                int palette = random.Next(3);
                Vector3 end = entry + direction * length;
                if (entry.sqrMagnitude < 87f * 87f || end.sqrMagnitude < 82f * 82f ||
                    OverCampaignMine(entry) || OverCampaignMine(end)) continue;

                float minCover = float.PositiveInfinity;
                Vector3 side = Vector3.Cross(Vector3.up, direction);
                for (float z = RampLength; z <= length + 0.01f; z += 2.5f)
                {
                    for (float x = -width; x <= width + 0.01f; x += width * 0.5f)
                    {
                        Vector3 sample = entry + direction * z + side * x;
                        minCover = Mathf.Min(minCover, GroundHeight(sample.x, sample.z));
                    }
                }
                for (int x = -1; x <= 1; x++)
                {
                    Vector3 sample = end + side * (x * width);
                    minCover = Mathf.Min(minCover, GroundHeight(sample.x, sample.z));
                }
                float floor = minCover - roof - 1.2f;
                float entrance = GroundHeight(entry.x, entry.z);
                if (entrance - floor > 8.4f || floor > entrance - 2.7f) continue;
                var cave = new CaveSite(entry, direction, floor, length, width, roof, palette);
                if (ConflictsWithLegacyResources(sector, origin, cave)) continue;
                return cave;
            }
            return null;
        }

        private static bool OverCampaignMine(Vector3 point) =>
            Mathf.Abs(point.x) < 22f && point.z > 9f && point.z < 196f;

        private bool ConflictsWithLegacyResources(Vector2Int sector, Vector3 origin, CaveSite cave)
        {
            var random = LegacyResourceRandom(sector);
            for (int slot = 0; slot < LegacyResourceSlots; slot++)
            {
                Vector3 point = LegacyResourcePoint(origin, slot, random);
                random.NextDouble(); // Resource kind.
                random.NextDouble(); // Yaw.
                if (cave.BlocksDressingAt(point.x, point.z)) return true;
            }
            return false;
        }

        private void BuildCave(Vector2Int sector, Transform root, BoreatekLandscapeGeometry owner, CaveSite site)
        {
            _caves[sector] = site;
            Color crystal = site.Palette switch
            {
                0 => new Color32(74, 194, 235, 255),
                1 => new Color32(175, 111, 223, 255),
                _ => new Color32(237, 127, 87, 255)
            };
            Material slate = _sharedMaterials.Finish(new Color32(64, 82, 103, 255));
            Material stone = _sharedMaterials.Finish(new Color32(86, 108, 132, 255));
            Material ice = _sharedMaterials.Finish(Color.Lerp(new Color32(44, 73, 103, 255), crystal, 0.18f), 0.07f, 0.51f);
            var shell = new Batch();
            BuildCaveRamp(shell, site, origin: root.position, stone, slate);
            BuildCaveChamber(shell, site, root.position, slate, stone, ice);
            owner.Place(root, "Cueva_Procedural_Rampa_Y_Camara", shell, collision: true);
            BuildRockOverhang(root, owner, site, slate, stone, ice);

            var random = new System.Random(CellSeed(_seed ^ 0x5CB17, sector.x, sector.y));
            for (int slot = 0; slot < CaveResourceSlots; slot++)
            {
                float side = slot % 2 == 0 ? -1f : 1f;
                float along = Mathf.Lerp(20.3f, site.Length - 2.2f, (slot / 2) * 0.5f);
                Vector3 point = site.At(side * (2.6f + (float)random.NextDouble() * 1.65f), along, site.Floor);
                string key = slot % 3 == 0 ? "coal_vein" : slot % 3 == 1 ? "cave_candy_vein" : "polar_iron_vein";
                HarvestableNode vein = _builder.SpawnMineralVeinNode(root, point, key, crystal, 0.8f, owner);
                vein.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                AttachResource(sector, LegacyResourceSlots + ScenerySlots + slot, vein);
            }
            var trim = new Batch();
            Material beacon = _sharedMaterials.Finish(crystal, 0.78f, 0.4f);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 p = site.At(side * 4.45f, 0.8f, 0f);
                p.y = GroundHeight(p.x, p.z) - 0.12f;
                trim.Frustum(p - root.position, p - root.position + Vector3.up * 1.2f + site.Side * (side * 0.23f),
                    0.46f, 0f, 6, beacon);
                Vector3 stalactite = site.At(side * 4.7f, 23f, site.Floor + site.Roof - 0.15f) - root.position;
                trim.Frustum(stalactite, stalactite - Vector3.up * 1.46f + site.Side * 0.22f, 0.66f, 0.03f, 6, ice);
            }
            owner.Place(root, "Cave_Crystal_Entrance_And_Ceiling", trim, collision: true);
            var lantern = new GameObject("Crystal_Chamber_Light");
            lantern.transform.SetParent(root, false);
            lantern.transform.position = site.At(0f, 24f, site.Floor + 2.8f);
            var light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = crystal;
            light.intensity = 3.4f;
            light.range = 13f;
            light.shadows = LightShadows.None;
        }

        private void BuildRockOverhang(Transform root, BoreatekLandscapeGeometry owner, CaveSite cave,
            Material shadow, Material rock, Material ice)
        {
            var random = new System.Random(CellSeed(_seed ^ 0x549C3, Mathf.RoundToInt(cave.Entry.x), Mathf.RoundToInt(cave.Entry.z)));
            Material snow = _sharedMaterials.Finish(new Color32(188, 212, 231, 255));
            Material drift = _sharedMaterials.Finish(new Color32(152, 180, 207, 255));
            var cover = new Batch();
            Vector3 origin = root.position;
            const int rim = 9;
            float[] depths = { 0.75f, 2.1f, 4.35f, 7.4f, 10.8f, 13.7f, 16.1f };
            var inner = new Vector3[depths.Length, rim];
            var outer = new Vector3[depths.Length, rim];
            float lean = (float)random.NextDouble() * 0.72f - 0.36f;
            for (int row = 0; row < depths.Length; row++)
            {
                float depth = depths[row];
                float retreat = Mathf.SmoothStep(0f, 1f, depth / RampLength);
                float heading = lean + Mathf.Sin(depth * 0.46f) * 0.32f;
                float opening = Mathf.Lerp(2.05f, 3.65f, retreat);
                float roof = Mathf.Lerp(2.85f, Mathf.Min(cave.Roof, 4.5f), retreat);
                float span = 5.05f + (float)random.NextDouble() * 0.95f;
                float bank = Mathf.Lerp(4.75f, 0.32f, retreat) + (float)random.NextDouble() * (1f - retreat);
                for (int i = 0; i < rim; i++)
                {
                    float angle = i * Mathf.PI / (rim - 1);
                    float contour = Mathf.Cos(angle), height = Mathf.Max(0f, Mathf.Sin(angle));
                    float irregularity = ((float)random.NextDouble() - 0.5f) * (i == 0 || i == rim - 1 ? 0.12f : 0.73f);
                    float layerDepth = depth + irregularity * (row == depths.Length - 1 ? 0.2f : 1f);
                    float cross = heading + contour * (opening + irregularity * 0.40f);
                    float y = RampPoint(cave, cross, Mathf.Clamp(layerDepth, 0f, RampLength)).y;
                    inner[row, i] = cave.At(cross, layerDepth, y - 0.10f +
                        Mathf.Pow(height, 0.74f) * (roof + irregularity * 0.47f)) - origin;

                    float coverCross = contour * (span + irregularity) + heading * 0.60f;
                    Vector3 outside = OnSnow(cave.At(coverCross, layerDepth, 0f));
                    float surface = outside.y + Mathf.Pow(height, 1.21f) * (bank + irregularity * 0.73f) - 0.25f;
                    if (i > 0 && i < rim - 1)
                        surface = Mathf.Max(surface, inner[row, i].y + origin.y + 0.58f);
                    outer[row, i] = cave.At(coverCross, layerDepth - irregularity * 0.65f, surface) - origin;
                }
            }

            for (int i = 0; i < rim - 1; i++)
            {
                Material face = i is 2 or 5 ? ice : (i % 2 == 0 ? rock : shadow);
                cover.Triangle(inner[0, i], inner[0, i + 1], outer[0, i], face);
                cover.Triangle(inner[0, i + 1], outer[0, i + 1], outer[0, i], i is 3 or 4 ? drift : face);
            }
            for (int row = 0; row < depths.Length - 1; row++)
            {
                for (int i = 0; i < rim - 1; i++)
                {
                    Material outside = i is >= 2 and <= 5 ? (i + row) % 4 == 1 ? drift : snow : rock;
                    cover.Quad(outer[row, i], outer[row, i + 1], outer[row + 1, i + 1], outer[row + 1, i], outside);
                    cover.Quad(inner[row, i], inner[row + 1, i], inner[row + 1, i + 1], inner[row, i + 1],
                        (i + row * 3) % 7 == 0 ? ice : shadow);
                }
                cover.Quad(outer[row, 0], outer[row + 1, 0], inner[row + 1, 0], inner[row, 0], rock);
                cover.Quad(inner[row, rim - 1], inner[row + 1, rim - 1], outer[row + 1, rim - 1], outer[row, rim - 1], rock);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float lateral = side * (4.5f + i * 0.78f + (float)random.NextDouble() * 0.44f);
                    Vector3 footing = OnSnow(cave.At(lateral, -0.36f + i * 2.16f, 0f)) - origin - Vector3.up * 0.52f;
                    float size = 1.40f + (float)random.NextDouble() * 0.54f;
                    Vector3 ridge = footing + (Vector3.up * (1.05f + size) - cave.Side * (0.49f * side));
                    cover.Frustum(footing, ridge, size, size * 0.58f, 6, i % 2 == 0 ? rock : shadow);
                    cover.Frustum(ridge, ridge + Vector3.up * 0.32f, size * 0.60f, 0.13f, 6, i == 0 ? drift : snow);
                }
            }
            owner.Place(root, "Saliente_Nevado_Entrada_De_Cueva", cover, collision: true);
        }

        private void BuildCaveRamp(Batch shell, CaveSite site, Vector3 origin, Material path, Material rock)
        {
            for (int depth = 0; depth < RampLength; depth += VertexStep)
            {
                float next = depth + VertexStep;
                for (int cross = -4; cross < 4; cross += VertexStep)
                    shell.Quad(RampPoint(site, cross, depth) - origin, RampPoint(site, cross, next) - origin,
                        RampPoint(site, cross + VertexStep, next) - origin, RampPoint(site, cross + VertexStep, depth) - origin, path);

                Vector3 left = RampPoint(site, -4f, depth), leftEnd = RampPoint(site, -4f, next);
                Vector3 right = RampPoint(site, 4f, depth), rightEnd = RampPoint(site, 4f, next);
                shell.Quad(left - origin, OnSnow(left) - origin, OnSnow(leftEnd) - origin, leftEnd - origin, rock);
                shell.Quad(rightEnd - origin, OnSnow(rightEnd) - origin, OnSnow(right) - origin, right - origin, rock);
            }

            for (int cross = -4; cross < 4; cross += VertexStep)
            {
                Vector3 left = site.At(cross, RampLength, site.Floor + site.Roof);
                Vector3 right = site.At(cross + VertexStep, RampLength, site.Floor + site.Roof);
                shell.Quad(left - origin, OnSnow(left) - origin, OnSnow(right) - origin, right - origin, rock);
            }
        }

        private Vector3 RampPoint(CaveSite cave, float cross, float depth)
        {
            Vector3 entry = cave.At(cross, 0f, 0f);
            float height = Mathf.Lerp(GroundHeight(entry.x, entry.z), cave.Floor, depth / RampLength);
            return cave.At(cross, depth, height);
        }

        private Vector3 OnSnow(Vector3 point)
        {
            point.y = GroundHeight(point.x, point.z);
            return point;
        }

        private static void BuildCaveChamber(Batch shell, CaveSite cave, Vector3 origin,
            Material rock, Material path, Material ice)
        {
            float[] stations = { RampLength, 20f, (20f + cave.Length) * 0.5f, cave.Length };
            float[] widths = { MouthHalfWidth, cave.Width * 0.91f, cave.Width, cave.Width * 0.77f };
            for (int i = 0; i < stations.Length - 1; i++)
            {
                float from = stations[i], to = stations[i + 1], a = widths[i], b = widths[i + 1];
                Vector3 left = cave.At(-a, from, cave.Floor) - origin;
                Vector3 right = cave.At(a, from, cave.Floor) - origin;
                Vector3 leftEnd = cave.At(-b, to, cave.Floor) - origin;
                Vector3 rightEnd = cave.At(b, to, cave.Floor) - origin;
                Vector3 up = Vector3.up * cave.Roof;
                float roofInset = i == 0 ? 0f : 0.42f;
                Vector3 leftTop = left + up + cave.Side * roofInset;
                Vector3 rightTop = right + up - cave.Side * roofInset;
                Vector3 leftEndTop = leftEnd + up + cave.Side * 0.42f;
                Vector3 rightEndTop = rightEnd + up - cave.Side * 0.42f;
                shell.Quad(left, leftEnd, rightEnd, right, path);
                shell.Quad(left, leftTop, leftEndTop, leftEnd, i % 2 == 0 ? ice : rock);
                shell.Quad(rightEnd, rightEndTop, rightTop, right, i % 2 == 0 ? rock : ice);
                shell.Quad(leftTop, rightTop, rightEndTop, leftEndTop, ice);
                if (i == stations.Length - 2)
                    shell.Quad(leftEnd, leftEndTop, rightEndTop, rightEnd, rock);
            }
        }
    }
}
