using Boreatek.Core;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public partial class BoreatekWorldBuilder
    {
        private BoreatekLandscapeGeometry _landscape;

        private static readonly Color32 Frost = new Color32(203, 230, 247, 255);
        private static readonly Color32 Steel = new Color32(39, 59, 72, 255);
        private static readonly Color32 Brass = new Color32(172, 107, 63, 255);
        private static readonly Color32 Furnace = new Color32(255, 131, 43, 255);

        private Material[] CreateSnowPalette() => new Material[]
        {
                _landscape.Finish(new Color32(172, 202, 221, 255)),
                _landscape.Finish(new Color32(187, 215, 232, 255)),
                _landscape.Finish(Frost),
                _landscape.Finish(new Color32(219, 239, 247, 255))
        };

        private void BuildArcticSurfaceTerrain(Transform parent)
        {
            Material[] snow = CreateSnowPalette();
            var terrain = new Batch();
            const int step = 4;
            for (int z = -126; z < 126; z += step)
            {
                for (int x = -124; x < 124; x += step)
                {
                    // The first ramp must descend below the snow deck before the deck closes above it.
                    if (x >= -4 && x < 4 && z >= 14 && z < 34) continue;
                    Vector3 a = SurfacePoint(x, z), b = SurfacePoint(x + step, z);
                    Vector3 c = SurfacePoint(x + step, z + step), d = SurfacePoint(x, z + step);
                    terrain.Triangle(a, d, b, snow[SnowBand(x + 1f, z + 1f)]);
                    terrain.Triangle(b, d, c, snow[SnowBand(x + 3f, z + 3f)]);
                }
            }
            _landscape.Place(parent, "ArcticSnowfields", terrain, collision: true);
            BuildArcticHorizon(parent);
        }

        internal static int SnowBand(float x, float z) =>
            Mathf.Clamp(Mathf.FloorToInt(Mathf.PerlinNoise(x * 0.113f + 32f, z * 0.121f + 42f) * 4f), 0, 3);

        internal static Vector3 SurfacePoint(float x, float z)
        {
            float radius = new Vector2(x, z).magnitude;
            float clearPlaza = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(21f, 47f, radius));
            float drift = (Mathf.PerlinNoise(x * 0.064f + 38f, z * 0.1f + 81f) - 0.55f) * 0.28f;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, 124f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))));
            float relief = edge * Mathf.PerlinNoise(x * 0.024f + 112f, z * 0.048f + 54f) * 8f;
            return new Vector3(x, clearPlaza * drift + relief, z);
        }

        private void BuildArcticHorizon(Transform parent)
        {
            Material rock = _landscape.Finish(new Color32(71, 101, 123, 255));
            Material ridgeSnow = _landscape.Finish(new Color32(167, 200, 219, 255));
            // Visual randomness stays separate from the seed that determines save-compatible resource indices.
            var art = new System.Random(_worldSeed ^ 3217);
            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                var ridge = new Batch();
                for (int i = 0; i < 6; i++)
                {
                    float angle = (quadrant * 6 + i) * 2f * Mathf.PI / 24f;
                    var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 foot = radial * (125f + (float)art.NextDouble() * 40f) - Vector3.up * 3f;
                    Vector3 crown = foot + radial * 8f + Vector3.up * (23f + (float)art.NextDouble() * 31f);
                    Vector3 seam = Vector3.Lerp(foot, crown, 0.53f);
                    float width = 20f + (float)art.NextDouble() * 24f;
                    ridge.Frustum(foot, seam, width, width * 0.44f, 6, rock);
                    ridge.Frustum(seam, crown, width * 0.44f, 1.1f, 6, ridgeSnow);
                }
                _landscape.Place(parent, $"PolarRidge_{quadrant + 1}", ridge);
            }
        }

        private void PopulateSnowPineBelts(Transform parent)
        {
            var art = new System.Random(_worldSeed ^ 53107);
            for (int belt = 0; belt < 4; belt++)
            {
                var trees = new Batch();
                for (int i = 0; i < 18; i++)
                {
                    float z = -105f + (float)art.NextDouble() * 206f;
                    float x = -48f - belt * 12f - (float)art.NextDouble() * 13f;
                    float height = 3.8f + (float)art.NextDouble() * 4.5f;
                    AddFrostedPine(trees, SurfacePoint(x, z) - Vector3.up * 0.12f, height, marked: false);
                }
                _landscape.Place(parent, $"SnowPineBelt_{belt + 1}", trees);
            }
        }

        internal void AddFrostedPine(Batch trees, Vector3 foot, float height, bool marked)
        {
            Material bark = _landscape.Finish(new Color32(77, 58, 55, 255));
            Material pine = _landscape.Finish(new Color32(45, 87, 95, 255));
            Material snow = _landscape.Finish(Frost);
            trees.Frustum(foot, foot + Vector3.up * (height * 0.8f), height * 0.065f, height * 0.025f, 6, bark);
            for (int tier = 0; tier < 4; tier++)
            {
                float level = 0.21f + tier * 0.16f;
                float radius = height * (0.26f - tier * 0.048f);
                Vector3 basePoint = foot + Vector3.up * (height * level);
                Vector3 tip = basePoint + Vector3.up * (height * 0.33f);
                trees.Frustum(basePoint, tip, radius, 0f, 7, pine);
                trees.Frustum(basePoint + Vector3.up * (height * 0.045f), tip + Vector3.up * 0.08f,
                    radius * 0.95f, 0f, 7, snow);
            }
            if (marked)
            {
                Material ice = _landscape.Finish(new Color32(88, 191, 225, 255), 0.35f);
                Vector3 shard = foot + new Vector3(0.34f, 0.62f, -0.25f);
                trees.Frustum(shard, shard + new Vector3(0.23f, 1.1f, -0.08f), 0.20f, 0f, 5, ice);
            }
        }

        private void DressVillagePlaza(Transform village)
        {
            var plaza = new Batch();
            Material curb = _landscape.Finish(Steel);
            Material packedSnow = _landscape.Finish(new Color32(146, 177, 193, 255));
            Material track = _landscape.Finish(new Color32(96, 126, 143, 255));
            Material glow = _landscape.Finish(Furnace, 2.1f);
            for (int i = 0; i < 11; i++)
            {
                float z = -13f + i * 1.65f;
                plaza.Box(new Vector3(0f, 0.165f, z), new Vector3(3.45f, 0.04f, 1.58f), packedSnow);
                for (int side = -1; side <= 1; side += 2)
                {
                    plaza.Box(new Vector3(side * 0.93f, 0.19f, z), new Vector3(0.16f, 0.018f, 1.6f), track);
                }
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 foot = new Vector3(side * 3.45f, 0.18f, -10.5f + i * 4.1f);
                    plaza.Frustum(foot, foot + Vector3.up * 2.25f, 0.14f, 0.095f, 6, curb);
                    plaza.Box(foot + Vector3.up * 2.29f, new Vector3(0.52f, 0.23f, 0.48f), glow);
                    plaza.Box(foot + Vector3.up * 2.49f, new Vector3(0.62f, 0.17f, 0.58f), curb);
                }
                for (int i = 0; i < 5; i++)
                {
                    Vector3 foot = SurfacePoint(side * (12.5f + i * 1.45f), -6.5f + i * 3.2f);
                    AddFrostedPine(plaza, foot, 3.7f + 0.42f * (i % 3), marked: false);
                }
            }
            _landscape.Place(village, "ForgeApproach_TracksAndLanterns", plaza);
        }

        private void DressTower(Transform tower)
        {
            var kit = new Batch();
            Material casing = _landscape.Finish(Steel, smoothness: 0.34f);
            Material pipe = _landscape.Finish(Brass, smoothness: 0.42f);
            Material heatedGlass = _landscape.Finish(Furnace, 1.8f);
            kit.Frustum(Vector3.up * 14.4f, Vector3.up * 14.7f, 2.71f, 2.68f, 16, casing);
            kit.Frustum(Vector3.up * 15.96f, Vector3.up * 16.14f, 2.71f, 2.68f, 16, casing);
            for (int i = 0; i < 12; i++)
            {
                float angle = 30f * i;
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 radial = rotation * Vector3.forward;
                kit.Box(radial * 2.66f + Vector3.up * 15.24f, new Vector3(0.15f, 1.35f, 0.19f), casing, rotation);
                if (i % 3 != 0) continue;
                kit.Frustum(radial * 1.82f + Vector3.up * 1.2f, radial * 1.82f + Vector3.up * 13.6f, 0.13f, 0.13f, 7, pipe);
                kit.Box(radial * 1.83f + Vector3.up * 5.4f, new Vector3(0.27f, 0.61f, 0.09f), heatedGlass, rotation);
                kit.Box(radial * 1.82f + Vector3.up * 1.07f, new Vector3(1.7f, 2.1f, 0.68f), casing, rotation);
            }
            kit.Frustum(Vector3.up * 16.85f, Vector3.up * 18.1f, 0.13f, 0.10f, 6, casing);
            _landscape.Place(tower, "Tower_CopperServicesAndCrown", kit);
        }

        private void BuildForgeMachinery(Transform root)
        {
            var assembly = new Batch();
            Material casing = _landscape.Finish(Steel);
            Material copper = _landscape.Finish(Brass, smoothness: 0.5f);
            Material amber = _landscape.Finish(Furnace, 2.45f);
            Material coils = _landscape.Finish(new Color32(198, 60, 18, 255), 1.25f);
            Vector3 center = new Vector3(0f, 4.8f, -0.79f);
            assembly.Ring(center, 3.42f, 4.04f, 32, casing);
            assembly.Ring(center - Vector3.forward * 0.045f, 3.22f, 3.40f, 32, copper);
            assembly.Ring(center - Vector3.forward * 0.10f, 3.09f, 3.16f, 32, coils);
            assembly.Ring(center - Vector3.forward * 0.09f, 1.47f, 1.58f, 24, amber);
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 22.5f;
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                Vector3 radial = rotation * Vector3.up;
                assembly.Box(center + radial * 3.7f - Vector3.forward * 0.12f, new Vector3(0.23f, 0.46f, 0.19f), copper, rotation);
                if (i % 2 == 0)
                {
                    assembly.Box(center + radial * 2.3f - Vector3.forward * 0.055f,
                        new Vector3(0.085f, 1.3f, 0.09f), coils, rotation);
                }
            }
            assembly.Frustum(new Vector3(0f, 1.24f, -1.55f), new Vector3(0f, 1.73f, -1.55f), 2.45f, 2.13f, 18, casing);
            assembly.Frustum(new Vector3(0f, 1.73f, -1.55f), new Vector3(0f, 1.87f, -1.55f), 1.85f, 1.71f, 18, amber);
            _landscape.Place(root, "Forge_RingsCoilsAndHearth", assembly);
        }

        internal HarvestableNode CreateFrontierResource(Transform parent, BoreatekLandscapeGeometry owner, Vector3 position,
            BiomeType biome, float type, float yaw)
        {
            if (biome == BiomeType.GiantCandyForest) return SpawnGiantCandyCrystalTreeNode(parent, position, owner);
            if (biome == BiomeType.SleighGraveyard && type > 0.56f) return SpawnFrozenChestNode(parent, position);
            if (biome == BiomeType.SleighGraveyard || type > 0.85f)
            {
                return SpawnCrashedSleighScrapNode(parent, position, owner, Quaternion.Euler(-4f, yaw, 3f));
            }
            return SpawnCrystallizedPineNode(parent, position, owner);
        }

        private void DressCandyTree(Transform tree, Vector3 location, BoreatekLandscapeGeometry owner)
        {
            Material red = _landscape.Finish(new Color32(163, 38, 65, 255), 0.22f, 0.45f);
            Material ivory = _landscape.Finish(new Color32(255, 217, 187, 255), 0.12f, 0.30f);
            Material ruby = _landscape.Finish(new Color32(231, 47, 76, 255), 0.65f, 0.49f);
            Material pink = _landscape.Finish(new Color32(252, 117, 111, 255), 0.45f, 0.46f);
            float growth = Mathf.Lerp(0.84f, 1.3f, Mathf.PerlinNoise(location.x * 0.23f, location.z * 0.3f));
            float yaw = Mathf.PerlinNoise(location.x * 0.56f, location.z * 0.17f) * Mathf.PI * 2f;
            var canopy = new Batch();

            Vector3 flare = Vector3.up * (0.85f * growth);
            Vector3 fork = new Vector3(0.17f, 2.55f, -0.13f) * growth;
            canopy.Frustum(Vector3.zero, flare, 0.87f * growth, 0.44f * growth, 8, red);
            canopy.Frustum(flare, fork, 0.45f * growth, 0.32f * growth, 8, ivory);
            for (int i = 0; i < 5; i++)
            {
                float angle = yaw + i * 2f * Mathf.PI / 5f;
                var side = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 shoulder = fork + (side * 1.05f + Vector3.up * (0.65f + 0.13f * i)) * growth;
                Vector3 tip = fork + (side * (1.7f + i % 2 * 0.43f) + Vector3.up * (1.76f + 0.11f * i)) * growth;
                canopy.Frustum(fork, shoulder, 0.23f * growth, 0.17f * growth, 6, ivory);
                canopy.Frustum(shoulder, tip, 0.175f * growth, 0.08f * growth, 5, (i % 2 == 0 ? red : ivory));
                for (int branch = -1; branch <= 1; branch++)
                {
                    Vector3 branchEnd = tip + (Vector3.up * (0.87f - 0.21f * Mathf.Abs(branch)) +
                        (side + Vector3.Cross(Vector3.up, side) * branch) * 0.55f) * growth;
                    Vector3 facet = Vector3.Lerp(tip, branchEnd, 0.4f);
                    canopy.Frustum(tip, facet, 0.11f * growth, 0.32f * growth, 5, ruby);
                    canopy.Frustum(facet, branchEnd, 0.32f * growth, 0f, 5, (branch == 0 ? pink : ruby));
                }
            }
            owner.Place(tree, "CandyTree_BranchesAndCrystals", canopy);
        }

        private void DressSleigh(Transform sleigh, BoreatekLandscapeGeometry owner)
        {
            var wreck = new Batch();
            Material enamel = _landscape.Finish(new Color32(132, 43, 57, 255), smoothness: 0.35f);
            Material gold = _landscape.Finish(new Color32(195, 139, 71, 255), smoothness: 0.56f);
            Material timber = _landscape.Finish(new Color32(86, 63, 57, 255));
            Material snow = _landscape.Finish(Frost);
            wreck.Box(new Vector3(0f, 0.48f, -0.15f), new Vector3(1.64f, 0.37f, 2.32f), timber);
            wreck.Box(new Vector3(0f, 0.82f, -1.18f), new Vector3(1.75f, 0.9f, 0.22f), enamel);
            wreck.Box(new Vector3(0f, 1.31f, -1.18f), new Vector3(1.84f, 0.10f, 0.30f), gold);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.77f;
                wreck.Box(new Vector3(x, 0.73f, -0.14f), new Vector3(0.18f, 0.54f, 2.22f), enamel);
                wreck.Box(new Vector3(x, 1.04f, -0.20f), new Vector3(0.24f, 0.09f, 2.34f), gold);
                wreck.Box(new Vector3(x, 0.17f, -0.18f), new Vector3(0.17f, 0.14f, 2.83f), gold);
                wreck.Frustum(new Vector3(x, 0.17f, 1.23f), new Vector3(x, 0.51f, 1.62f), 0.10f, 0.075f, 6, gold);
                wreck.Box(new Vector3(x, 0.27f, -0.83f), new Vector3(0.11f, 0.35f, 0.16f), timber);
            }
            wreck.Box(new Vector3(-0.20f, 0.68f, -0.24f), new Vector3(0.71f, 0.42f, 0.88f), snow,
                Quaternion.Euler(0f, 13f, -6f));
            owner.Place(sleigh, "Sleigh_EnamelAndRunners", wreck);
        }

        private void DressGraveyard(Transform parent)
        {
            var ruins = new Batch();
            Material boards = _landscape.Finish(new Color32(92, 73, 71, 255));
            Material frost = _landscape.Finish(Frost);
            Material ice = _landscape.Finish(new Color32(136, 178, 204, 255));
            for (int i = 0; i < 5; i++)
            {
                // The shelters frame the western ridge outside the salvage route.
                Vector3 foot = SurfacePoint(-75f - i % 2 * 6f, -82f + i * 8.5f);
                ruins.Box(foot + Vector3.up * 0.9f, new Vector3(4.2f, 1.8f, 3.3f), boards);
                for (int side = -1; side <= 1; side += 2)
                {
                    ruins.Box(foot + new Vector3(side * 1.03f, 2.33f, 0f), new Vector3(2.65f, 0.23f, 3.8f), frost,
                        Quaternion.Euler(0f, 0f, side * -25f));
                }
                Vector3 drift = foot + new Vector3(0.7f, -0.6f, -1.8f);
                ruins.Frustum(drift, drift + Vector3.up * 0.88f, 2.1f, 0.4f, 6, ice);
            }
            _landscape.Place(parent, "Graveyard_FrozenShelters", ruins);
        }
    }
}
