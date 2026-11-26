using Boreatek.Core;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private const int CaveResourceSlots = 6;
        private const int DeepCaveResourceSlots = 6;
        private const int RampLength = 16;
        private const float MouthHalfWidth = 4f;
        private readonly System.Collections.Generic.Dictionary<Vector2Int, CaveLayout> _caveLayouts = new();

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

        internal bool IsInProceduralCave(Vector3 position) => ProceduralCaveLayer(position) != 0;

        internal int ProceduralCaveLayer(Vector3 position) =>
            _caveLayouts.TryGetValue(SectorAt(position), out CaveLayout layout) ? layout.LayerAt(position) : 0;

        private bool IsCaveHeatPlacement(Vector3 position, Vector2Int sector)
        {
            if (_caveLayouts.TryGetValue(sector, out CaveLayout loaded)) return loaded.LayerAt(position) != 0;
            if (_chunks.ContainsKey(sector)) return false;
            CaveSite? planned = PlanCave(sector, SectorOrigin(sector));
            return planned.HasValue && PlanCaveLayout(sector, planned.Value).LayerAt(position) != 0;
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
            CaveLayout layout = PlanCaveLayout(sector, site);
            _caveLayouts[sector] = layout;
            Color crystal = site.Palette switch
            {
                0 => new Color32(74, 194, 235, 255),
                1 => new Color32(175, 111, 223, 255),
                _ => new Color32(237, 127, 87, 255)
            };
            Material slate = _sharedMaterials.Finish(new Color32(64, 82, 103, 255));
            Material stone = _sharedMaterials.Finish(new Color32(86, 108, 132, 255));
            Material ice = _sharedMaterials.Finish(Color.Lerp(new Color32(44, 73, 103, 255), crystal, 0.18f), 0.07f, 0.51f);
            BuildCaveShell(root, owner, layout, slate, stone, ice);
            DressCaveFormations(root, owner, layout, slate, ice);
            DressDeepCave(sector, root, owner, layout, crystal);

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

    }
}
