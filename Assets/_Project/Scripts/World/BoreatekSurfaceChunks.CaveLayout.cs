using System.Collections.Generic;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private sealed class CaveLayout
        {
            internal CaveSite Site;
            internal CaveSection[] Sections;
            internal CaveBranch[] Branches;

            internal int LayerAt(Vector3 position)
            {
                Vector2 local = Site.Coordinates(position.x, position.z);
                for (int i = 1; i < Sections.Length; i++)
                {
                    var a = Sections[i - 1];
                    var b = Sections[i];
                    if (local.y < Mathf.Max(0.9f, a.Along) || local.y > b.Along) continue;
                    float t = Mathf.InverseLerp(a.Along, b.Along, local.y);
                    float floor = Mathf.Lerp(a.Floor, b.Floor, t);
                    float roof = Mathf.Lerp(a.Roof, b.Roof, t);
                    float center = Mathf.Lerp(a.Center, b.Center, t);
                    float width = Mathf.Lerp(a.Width, b.Width, t);
                    if (Mathf.Abs(local.x - center) <= width + 0.12f &&
                        position.y >= floor - 0.40f && position.y <= floor + roof + 0.20f)
                        return DepthLayer(floor);
                }
                foreach (var branch in Branches)
                {
                    if (branch.Contains(local, position.y, out float floor)) return DepthLayer(floor);
                }
                return 0;
            }

            private int DepthLayer(float floor) => floor < Site.Floor - 8.8f ? 4 : floor < Site.Floor - 3.8f ? 3 : 2;
        }

        private readonly struct CaveSection
        {
            internal readonly float Along, Center, Floor, Width, Roof;
            internal CaveSection(float along, float center, float floor, float width, float roof)
            {
                Along = along;
                Center = center;
                Floor = floor;
                Width = width;
                Roof = roof;
            }

            internal Vector3[] Ring(CaveSite site) => new[]
            {
                site.At(Center - Width, Along, Floor),
                site.At(Center, Along, Floor),
                site.At(Center + Width, Along, Floor),
                site.At(Center + Width, Along, Floor + Roof * 0.70f),
                site.At(Center + Width * 0.63f, Along, Floor + Roof * 0.91f),
                site.At(Center + Width * 0.09f, Along, Floor + Roof),
                site.At(Center - Width * 0.58f, Along, Floor + Roof * 0.92f),
                site.At(Center - Width, Along, Floor + Roof * 0.73f)
            };
        }

        private readonly struct CaveBranch
        {
            internal readonly int Interval, Side;
            internal readonly float Mouth, Along, Floor, Length, Bend, Drop;
            internal CaveBranch(int interval, int side, CaveSection a, CaveSection b, float length, float bend, float drop)
            {
                Interval = interval;
                Side = side;
                Mouth = (a.Center + b.Center + side * (a.Width + b.Width)) * 0.5f;
                Along = (a.Along + b.Along) * 0.5f;
                Floor = (a.Floor + b.Floor) * 0.5f;
                Length = length;
                Bend = bend;
                Drop = drop;
            }

            internal bool Contains(Vector2 point, float y, out float floor)
            {
                float extent = (point.x - Mouth) * Side;
                float t = Mathf.Clamp01(extent / Length);
                floor = Floor - Drop * t;
                return extent >= -1.4f && extent <= Length + 0.35f &&
                    Mathf.Abs(point.y - Along - Bend * t) <= Mathf.Lerp(2.3f, 3.4f, t) &&
                    y >= floor - 0.62f && y <= floor + 4.0f;
            }
        }

        private CaveLayout PlanCaveLayout(Vector2Int sector, CaveSite site)
        {
            var rng = new System.Random(CellSeed(_seed ^ 0x2C135D, sector.x, sector.y));
            float offset = RandomRange(rng, -0.56f, 0.56f);
            float deep = RandomRange(rng, 5.6f, 7.2f);
            float entrance = GroundHeight(site.Entry.x, site.Entry.z);
            var sections = new List<CaveSection>();
            for (int along = 0; along <= RampLength; along += VertexStep)
            {
                float t = along / (float)RampLength;
                sections.Add(new CaveSection(along, Mathf.Sin(t * Mathf.PI) * offset,
                    Mathf.Lerp(entrance, site.Floor, t), Mathf.Lerp(4f, 4.25f, t) - Mathf.Sin(t * Mathf.PI) * 1.03f,
                    Mathf.Lerp(2.95f, site.Roof, t)));
            }
            sections.Add(new CaveSection(20f, offset, site.Floor, site.Width * 0.92f, site.Roof + 0.18f));
            sections.Add(new CaveSection((20f + site.Length) * 0.5f, -offset * 0.55f, site.Floor, site.Width, site.Roof + 0.32f));
            sections.Add(new CaveSection(site.Length, 0f, site.Floor, site.Width * 0.80f, site.Roof));
            int turn = rng.Next(2) == 0 ? -1 : 1;
            float bend = RandomRange(rng, 1.15f, 2.4f) * turn;
            AddCaveExtension(site, sections, deep, bend);
            var branches = new List<CaveBranch>();
            AddCaveBranch(site, sections, branches, new[] { 5, 6, 7 }, turn, false, rng);
            AddCaveBranch(site, sections, branches, new[] { 8, 9, 7, 6, 5 }, -turn, true, rng);
            if (sections.Count > 9 && rng.NextDouble() < 0.52)
                AddCaveBranch(site, sections, branches, new[] { 9, 8 }, turn, false, rng);
            return new CaveLayout { Site = site, Sections = sections.ToArray(), Branches = branches.ToArray() };
        }

        private void AddCaveExtension(CaveSite site, List<CaveSection> sections, float depth, float bend)
        {
            var start = sections[^1];
            for (int attempt = 0; attempt < 9; attempt++)
            {
                float range = 46f - site.Length - (attempt % 3) * 2.75f;
                float curve = attempt < 3 ? bend : attempt < 6 ? -bend * 2.05f : bend * 3.45f;
                float drop = Mathf.Min(depth + attempt * 0.14f, range * 0.65f);
                var a = new CaveSection(start.Along + range * 0.37f, curve * 0.46f,
                    start.Floor - drop * 0.33f, 3f, site.Roof + 0.46f);
                var b = new CaveSection(start.Along + range * 0.74f, curve,
                    start.Floor - drop * 0.68f, 3.7f, site.Roof + 0.55f);
                var end = new CaveSection(start.Along + range, curve * 0.66f,
                    start.Floor - drop, Mathf.Min(6.2f, site.Width * 0.73f), site.Roof + 0.65f);
                if (!SectionHasCover(site, start, a) || !SectionHasCover(site, a, b) ||
                    !SectionHasCover(site, b, end)) continue;
                sections.Add(a);
                sections.Add(b);
                sections.Add(end);
                return;
            }
        }

        private bool SectionHasCover(CaveSite site, CaveSection start, CaveSection end)
        {
            for (int row = 0; row <= 8; row++)
            {
                float t = row / 8f;
                float along = Mathf.Lerp(start.Along, end.Along, t);
                float width = Mathf.Lerp(start.Width, end.Width, t);
                float center = Mathf.Lerp(start.Center, end.Center, t);
                float floor = Mathf.Lerp(start.Floor, end.Floor, t);
                float roof = Mathf.Lerp(start.Roof, end.Roof, t);
                for (int x = -4; x <= 4; x++)
                {
                    float across = x / 4f;
                    Vector3 p = site.At(center + width * across, along, floor + roof * (1f - Mathf.Abs(across) * 0.22f));
                    if (GroundHeight(p.x, p.z) < p.y + 0.45f) return false;
                }
            }
            return true;
        }

        private void AddCaveBranch(CaveSite site, List<CaveSection> sections, List<CaveBranch> branches,
            int[] intervals, int preferredSide, bool deep, System.Random rng)
        {
            foreach (int interval in intervals)
            {
                if (interval >= sections.Count - 1) continue;
                var a = sections[interval];
                var b = sections[interval + 1];
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int side = (attempt & 1) == 0 ? preferredSide : -preferredSide;
                    float floor = (a.Floor + b.Floor) * 0.5f;
                    float drop = deep ? Mathf.Max(RandomRange(rng, 5.3f, 7.4f), floor - site.Floor + 9.3f) :
                        RandomRange(rng, 1.15f, 3.5f);
                    float length = deep ? Mathf.Max(RandomRange(rng, 10.2f, 14.3f), drop * 1.49f) :
                        RandomRange(rng, 6.5f, 10.5f);
                    var candidate = new CaveBranch(interval, side, a, b, length,
                        RandomRange(rng, -3.8f, 3.4f), drop);
                    bool overlaps = branches.Exists(other => other.Side == side &&
                        Mathf.Abs(other.Along + other.Bend * 0.6f - candidate.Along - candidate.Bend * 0.6f) < 9.4f);
                    if (overlaps || !BranchHasCover(site, candidate, a, b)) continue;
                    branches.Add(candidate);
                    return;
                }
            }
        }

        private bool BranchHasCover(CaveSite site, CaveBranch branch, CaveSection a, CaveSection b)
        {
            Vector2Int sector = SectorAt(site.Entry);
            for (int row = 0; row <= 12; row++)
            {
                float t = row / 12f;
                float width = BranchWidth(t);
                float height = BranchRoof(t);
                float along = branch.Along + branch.Bend * t;
                float alongMin = Mathf.Lerp(a.Along, along - width, Mathf.Min(1f, t * 4f));
                float alongMax = Mathf.Lerp(b.Along, along + width, Mathf.Min(1f, t * 4f));
                for (int step = 0; step <= 6; step++)
                {
                    float across = step / 6f;
                    float start = Mathf.Lerp(a.Center + branch.Side * a.Width, b.Center + branch.Side * b.Width, across);
                    float x = Mathf.Lerp(start, branch.Mouth, Mathf.Min(1f, t * 4f)) + branch.Side * branch.Length * t;
                    float y = branch.Floor - branch.Drop * t +
                        Mathf.Lerp(Mathf.Lerp(a.Roof, b.Roof, across) * 0.73f, height, Mathf.Min(1f, t * 4f));
                    Vector3 p = site.At(x, Mathf.Lerp(alongMin, alongMax, across), y);
                    if (SectorAt(p) != sector || GroundHeight(p.x, p.z) < p.y + 0.55f) return false;
                }
            }
            return true;
        }

        private static float BranchWidth(float t) => 2f + 1.18f * Mathf.Sin(t * 2.4f);
        private static float BranchRoof(float t) => 2.9f + t * 0.52f;

        private static float RandomRange(System.Random rng, float low, float high) => Mathf.Lerp(low, high, (float)rng.NextDouble());

        private void BuildCaveShell(Transform root, BoreatekLandscapeGeometry owner, CaveLayout layout,
            Material rock, Material path, Material ice)
        {
            var shell = new Batch();
            Vector3 origin = root.position;
            var rings = new Vector3[layout.Sections.Length][];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = layout.Sections[i].Ring(layout.Site);
                if (i < 4)
                {
                    for (int foot = 0; foot < 3; foot++)
                    {
                        Vector3 rim = layout.Site.At((foot - 1) * MouthHalfWidth, 0f, 0f);
                        rings[i][foot].y = Mathf.Lerp(GroundHeight(rim.x, rim.z), layout.Site.Floor, i / 4f);
                    }
                }
                for (int j = 0; j < rings[i].Length; j++) rings[i][j] -= origin;
            }
            for (int i = 0; i < rings.Length - 1; i++)
            {
                var a = rings[i];
                var b = rings[i + 1];
                for (int edge = 0; edge < a.Length; edge++)
                {
                    int next = (edge + 1) % a.Length;
                    if (TryBuildBranch(shell, layout, i, edge, a, b, origin, path, rock, ice)) continue;
                    shell.Quad(a[edge], b[edge], b[next], a[next], edge < 2 ? path : (edge + i * 3) % 5 == 0 ? ice : rock);
                }
            }
            var last = rings[^1];
            Vector3 cap = (last[1] + last[5]) * 0.5f;
            for (int i = 0; i < last.Length; i++)
                shell.Triangle(last[(i + 1) % last.Length], last[i], cap, rock);
            AddCaveOverburden(shell, layout.Site, rings, origin, rock);
            owner.Place(root, "Cueva_Procedural_Rampa_Y_Camara", shell, collision: true);
        }

        private bool TryBuildBranch(Batch shell, CaveLayout layout, int interval, int edge, Vector3[] a,
            Vector3[] b, Vector3 origin, Material path, Material rock, Material ice)
        {
            if (edge != 2 && edge != 7) return false;
            foreach (var branch in layout.Branches)
            {
                if (branch.Interval != interval || branch.Side != (edge == 2 ? 1 : -1)) continue;
                int next = (edge + 1) % a.Length;
                Vector3[] previous = { a[edge], b[edge], b[next], a[next] };
                for (int row = 1; row <= 4; row++)
                {
                    float t = row * 0.25f;
                    float foot = branch.Floor - branch.Drop * t, z = branch.Along + t * branch.Bend;
                    float x = branch.Mouth + branch.Side * branch.Length * t;
                    float width = BranchWidth(t);
                    float roof = BranchRoof(t);
                    Vector3 near = layout.Site.At(x, z - width, foot) - origin;
                    Vector3 far = layout.Site.At(x, z + width, foot) - origin;
                    var current = branch.Side > 0
                        ? new[] { near, far, far + Vector3.up * roof, near + Vector3.up * (roof - 0.22f) }
                        : new[] { near + Vector3.up * roof, far + Vector3.up * (roof - 0.22f), far, near };
                    for (int face = 0; face < 4; face++)
                    {
                        int following = (face + 1) % 4;
                        shell.Quad(previous[face], previous[following], current[following], current[face],
                            face == (branch.Side > 0 ? 0 : 2) ? path : row % 2 == 0 ? rock : ice);
                    }
                    previous = current;
                }
                shell.Quad(previous[0], previous[1], previous[2], previous[3], rock);
                return true;
            }
            return false;
        }

        private void AddCaveOverburden(Batch shell, CaveSite site, Vector3[][] rings, Vector3 origin, Material rock)
        {
            Material snow = _sharedMaterials.Finish(new Color32(189, 212, 228, 255));
            Material drift = _sharedMaterials.Finish(new Color32(150, 173, 197, 255));
            int[] indices = { 2, 3, 4, 5, 6, 7, 0 };
            Vector3[] previous = null;
            for (int row = 0; row <= 4; row++)
            {
                var outer = new Vector3[indices.Length];
                for (int i = 0; i < outer.Length; i++)
                {
                    float arch = Mathf.Max(0f, Mathf.Sin(i * Mathf.PI / (outer.Length - 1)));
                    float across = Mathf.Cos(i * Mathf.PI / (outer.Length - 1)) * (5.75f + Mathf.Sin(row * 1.6f) * 0.62f);
                    Vector3 p = site.At(across, row * VertexStep, 0f);
                    float tundra = GroundHeight(p.x, p.z);
                    float mound = Mathf.Lerp(4.45f, 0.06f, row * 0.25f) + Mathf.Sin(i * 2.9f + row) * 0.45f;
                    p.y = tundra - 0.29f + arch * mound;
                    if (i > 0 && i < outer.Length - 1)
                        p.y = Mathf.Max(p.y, rings[row][indices[i]].y + origin.y + 0.44f);
                    outer[i] = p - origin;
                }
                for (int i = 0; i < outer.Length - 1; i++)
                {
                    if (row == 0)
                        shell.Quad(rings[row][indices[i]], rings[row][indices[i + 1]], outer[i + 1], outer[i], rock);
                    else
                        shell.Quad(previous[i], previous[i + 1], outer[i + 1], outer[i], i is 2 or 3 ? snow : drift);
                }
                if (row > 0)
                {
                    shell.Quad(previous[0], outer[0], rings[row][2], rings[row - 1][2], rock);
                    shell.Quad(rings[row - 1][0], rings[row][0], outer[^1], previous[^1], rock);
                }
                previous = outer;
            }
            for (int i = 0; i < indices.Length - 1; i++)
                shell.Quad(rings[4][indices[i + 1]], rings[4][indices[i]], previous[i], previous[i + 1], rock);
        }

        private void DressDeepCave(Vector2Int sector, Transform root, BoreatekLandscapeGeometry owner, CaveLayout layout, Color crystal)
        {
            var rng = new System.Random(CellSeed(_seed ^ 0x124BA7, sector.x, sector.y));
            int first = LegacyResourceSlots + ScenerySlots + CaveResourceSlots;
            var end = layout.Sections[^1];
            var approach = layout.Sections[^2];
            for (int i = 0; i < DeepCaveResourceSlots; i++)
            {
                Vector3 at;
                if (i < 2 || layout.Branches.Length == 0)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    float t = 0.78f - (i / 2) * 0.33f;
                    float width = Mathf.Lerp(approach.Width, end.Width, t);
                    at = layout.Site.At(Mathf.Lerp(approach.Center, end.Center, t) + side * (width - 1.43f),
                        Mathf.Lerp(approach.Along, end.Along, t), Mathf.Lerp(approach.Floor, end.Floor, t));
                }
                else
                {
                    int index = i - 2;
                    CaveBranch path = layout.Branches[index % layout.Branches.Length];
                    int row = index / layout.Branches.Length;
                    float t = 0.66f + (row / 2) * 0.19f;
                    float across = row % 2 == 0 ? -1.35f : 1.35f;
                    at = layout.Site.At(path.Mouth + path.Side * path.Length * t,
                        path.Along + path.Bend * t + across, path.Floor - path.Drop * t);
                }
                string mineral = i % 3 == 0 ? "polar_iron_vein" : i % 3 == 1 ? "cave_candy_vein" : "coal_vein";
                var vein = _builder.SpawnMineralVeinNode(root, at, mineral, Color.Lerp(crystal, new Color(0.32f, 0.83f, 1f), 0.44f), 0.82f, owner);
                vein.transform.rotation = Quaternion.Euler(0f, RandomRange(rng, 0f, 360f), 0f);
                AttachResource(sector, first + i, vein);
                if (i == 0 || i == 3)
                {
                    var lantern = new GameObject("Resplandor_De_Veta_Profunda");
                    lantern.transform.SetParent(root, false);
                    lantern.transform.position = at + Vector3.up * 1.95f;
                    var light = lantern.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 8.5f;
                    light.intensity = 1.8f;
                    light.color = crystal;
                }
            }
        }

        private static void DressCaveFormations(Transform root, BoreatekLandscapeGeometry owner, CaveLayout layout,
            Material rock, Material ice)
        {
            var details = new Batch();
            for (int i = 5; i < layout.Sections.Length; i++)
            {
                var section = layout.Sections[i];
                float side = i % 2 == 0 ? -1f : 1f;
                float wall = section.Center + side * section.Width * 0.57f;
                Vector3 mount = layout.Site.At(wall, section.Along - 0.21f, section.Floor + section.Roof * 0.92f) - root.position;
                float length = Mathf.Min(1.35f, section.Roof * 0.26f);
                details.Frustum(mount, mount - Vector3.up * length + layout.Site.Side * side * 0.18f,
                    0.23f + i % 3 * 0.12f, 0.015f, 6, i % 3 == 0 ? rock : ice);
                Vector3 partner = mount + layout.Site.Direction * 0.49f - layout.Site.Side * side * 0.24f;
                details.Frustum(partner, partner - Vector3.up * (length * 0.68f), 0.20f, 0.01f, 5, ice);
            }
            owner.Place(root, "Carambanos_Y_Estalactitas", details, collision: true);
        }
    }
}
