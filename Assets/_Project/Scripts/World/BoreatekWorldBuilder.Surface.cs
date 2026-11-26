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

        private Material CreateSnowMaterial()
        {
            return _landscape.Own(new Material(Resources.Load<Shader>("Boreatek/ArcticSnow")) { name = "Arctic snow" });
        }

        internal static Vector4 StarterSnowWeights(float x, float z)
        {
            float drift = (Mathf.PerlinNoise(x * 0.062f + 14f, z * 0.049f + 7f) - 0.5f) * 18f;
            float radius = new Vector2(x, z).magnitude;
            float village = 1f - EaseSnow(16f, 29f, radius + drift * 0.37f);
            float candy = EaseSnow(23f, 46f, x + drift) * (1f - village);
            float wreckage = Mathf.Max(EaseSnow(59f, 80f, radius + drift), EaseSnow(25f, 47f, -z - drift))
                * (1f - village - candy);
            return new Vector4(village, 1f - village - candy - wreckage, candy, wreckage);
        }

        internal static float EaseSnow(float start, float end, float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));

        internal static Color SnowTint(Vector4 weights, float x, float z)
        {
            Color snow = (Color)new Color32(146, 160, 168, 255) * weights.x
                + (Color)new Color32(172, 199, 219, 255) * weights.y
                + (Color)new Color32(187, 163, 198, 255) * weights.z
                + (Color)new Color32(137, 157, 178, 255) * weights.w;
            float detail = Mathf.PerlinNoise(x * 0.113f + 32f, z * 0.121f + 42f);
            return Color.Lerp(snow, Frost, detail * 0.69f);
        }

        internal static BiomeType SampleSnowBiome(Vector4 weights, float sample)
        {
            if ((sample -= weights.x) < 0f) return BiomeType.SantaIndustrialVillage;
            if ((sample -= weights.y) < 0f) return BiomeType.OuterTundra;
            return sample < weights.z ? BiomeType.GiantCandyForest : BiomeType.SleighGraveyard;
        }

        internal static void PaintSnowCell(Batch terrain, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material snow,
            Color inkA, Color inkB, Color inkC, Color inkD)
        {
            terrain.PaintTriangle(a, d, b, snow, inkA, inkD, inkB);
            terrain.PaintTriangle(b, d, c, snow, inkB, inkD, inkC);
        }

        internal static BiomeType StartingSurfaceBiome(float x, float z)
        {
            float radiusSquared = x * x + z * z;
            if (IsVillageGround(x, z)) return BiomeType.SantaIndustrialVillage;
            if (x > 32f) return BiomeType.GiantCandyForest;
            return z < -35f || radiusSquared > 68f * 68f ? BiomeType.SleighGraveyard : BiomeType.OuterTundra;
        }

        private void BuildArcticSurfaceTerrain(Transform parent)
        {
            Material snow = CreateSnowMaterial();
            var terrain = new Batch();
            const int step = 4;
            for (int z = -126; z < 126; z += step)
            {
                for (int x = -124; x < 124; x += step)
                {
                    if (IsMineEntranceCutout(x, z)) continue;
                    Vector3 a = SurfacePoint(x, z), b = SurfacePoint(x + step, z);
                    Vector3 c = SurfacePoint(x + step, z + step), d = SurfacePoint(x, z + step);
                    PaintSnowCell(terrain, a, b, c, d, snow, StarterSnowTint(a), StarterSnowTint(b),
                        StarterSnowTint(c), StarterSnowTint(d));
                }
            }
            _landscape.Place(parent, "ArcticSnowfields", terrain, collision: true);
            BuildArcticHorizon(parent);
        }

        private static Color StarterSnowTint(Vector3 p) => SnowTint(StarterSnowWeights(p.x, p.z), p.x, p.z);

        private void DressStarterBiomeLandmarks(Transform root)
        {
            var random = new System.Random(_worldSeed ^ 7639);
            var scenery = new Batch();
            for (int i = 0; i < 128; i++)
            {
                float x = (float)random.NextDouble() * 203f - 101f;
                float z = (float)random.NextDouble() * 196f - 99f;
                if (IsVillageGround(x, z) || x * x + z * z < 26f * 26f || (Mathf.Abs(x) < 5f && z >= 12f && z < 38f)) continue;
                Vector3 foot = StarterMeshFoot(x, z);
                Vector4 weights = StarterSnowWeights(x, z);
                BiomeType biome = SampleSnowBiome(weights, (float)random.NextDouble());
                float size = 0.64f + (float)random.NextDouble() * 0.58f;
                if (!IsAirfieldClearance(x, z)) AddBiomeTerrainDetail(scenery, foot, biome, size, _landscape);
                if (weights.z > 0.10f && weights.z < 0.84f && biome == BiomeType.OuterTundra && i % 4 == 0)
                    SpawnSceneryPine(root, StarterMeshFoot(x, z - 1.1f), 2.5f + size, $"edge:{i}");
            }
            _landscape.Place(root, "Snow_Drifts_Candy_Shoots_And_Salvage", scenery, collision: true);
        }

        private static Vector3 StarterMeshFoot(float x, float z)
        {
            const float step = 4f;
            float tx = Mathf.Floor(x / step) * step;
            float tz = Mathf.Floor((z + 2f) / step) * step - 2f;
            float u = (x - tx) / step, v = (z - tz) / step;
            float b = SurfacePoint(tx + step, tz).y, d = SurfacePoint(tx, tz + step).y;
            float y = u + v <= 1f
                ? (1f - u - v) * SurfacePoint(tx, tz).y + u * b + v * d
                : (u + v - 1f) * SurfacePoint(tx + step, tz + step).y + (1f - v) * b + (1f - u) * d;
            return new Vector3(x, y, z);
        }

        internal void AddBiomeTerrainDetail(Batch scenery, Vector3 foot, BiomeType biome, float size,
            BoreatekLandscapeGeometry materials, Vector3? groundNormal = null)
        {
            Quaternion slope = groundNormal.HasValue ? Quaternion.FromToRotation(Vector3.up, groundNormal.Value) : Quaternion.identity;
            foot -= Vector3.up * (groundNormal.HasValue ? 0.13f : 0f);
            if (biome == BiomeType.GiantCandyForest)
            {
                Material pinkIce = materials.Finish(new Color32(159, 52, 89, 255), 0.12f, 0.4f);
                Material sugar = materials.Finish(new Color32(235, 141, 184, 255), 0.18f);
                Material pale = materials.Finish(new Color32(185, 155, 203, 255));
                scenery.Frustum(foot - slope * Vector3.up * 0.12f, foot + slope * Vector3.up * 0.07f,
                    size * 0.66f, size * 0.45f, 7, pale);
                AddCrystal(scenery, foot, foot + slope * new Vector3(-0.29f, 0.77f, 0.16f) * size, size * 0.17f, pinkIce, sugar);
                AddCrystal(scenery, foot + slope * Vector3.right * size * 0.20f, foot + slope * new Vector3(0.47f, 0.54f, 0.05f) * size,
                    size * 0.14f, pinkIce, sugar);
            }
            else if (biome == BiomeType.SleighGraveyard)
            {
                Material timber = materials.Finish(new Color32(87, 67, 69, 255));
                Material rust = materials.Finish(new Color32(143, 66, 53, 255));
                Material snow = materials.Finish(new Color32(151, 173, 190, 255));
                scenery.Frustum(foot - slope * Vector3.up * 0.16f, foot + slope * new Vector3(0.22f, 0.22f, -0.1f) * size,
                    size * 0.75f, size * 0.43f, 6, snow);
                scenery.Box(foot + slope * new Vector3(-0.04f, 0.22f, 0.04f) * size, new Vector3(1.04f, 0.15f, 0.18f) * size,
                    timber, slope * Quaternion.Euler(17f, -35f, 16f));
                scenery.Box(foot + slope * new Vector3(0.37f, 0.31f, 0.22f) * size, new Vector3(0.20f, 0.09f, 0.22f) * size,
                    rust, slope * Quaternion.Euler(17f, -35f, 16f));
            }
            else
            {
                Material ice = materials.Finish(new Color32(81, 149, 185, 255), smoothness: 0.30f);
                Material snow = materials.Finish(new Color32(183, 216, 232, 255));
                scenery.Frustum(foot - slope * Vector3.up * 0.12f, foot + slope * new Vector3(size * 0.38f, 0.22f, 0.16f),
                    size * 0.82f, size * 0.31f, 7, snow);
                AddCrystal(scenery, foot + slope * Vector3.right * size * 0.24f, foot + slope * new Vector3(0.62f, 0.63f, 0.18f) * size,
                    size * 0.16f, ice, snow);
            }
        }

        internal static Vector3 SurfacePoint(float x, float z)
        {
            float radius = new Vector2(x, z).magnitude;
            float clearPlaza = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(21f, 47f, radius));
            float drift = (Mathf.PerlinNoise(x * 0.064f + 38f, z * 0.1f + 81f) - 0.55f) * 0.28f;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, 124f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))));
            float relief = edge * Mathf.PerlinNoise(x * 0.024f + 112f, z * 0.048f + 54f) * 8f;
            return new Vector3(x, (clearPlaza * drift + relief) * (1f - RunwayGrade(x, z)), z);
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
                for (int i = 0; i < 18; i++)
                {
                    float z = -105f + (float)art.NextDouble() * 206f;
                    float x = -48f - belt * 12f - (float)art.NextDouble() * 13f;
                    float height = 3.8f + (float)art.NextDouble() * 4.5f;
                    SpawnSceneryPine(parent, SurfacePoint(x, z), height, $"belt:{belt}:{i}");
                }
            }
        }

        internal HarvestableNode SpawnSceneryPine(Transform parent, Vector3 position, float height, string sceneryId = null,
            bool birch = false)
        {
            position = KeepClearOfVillageBuildings(position);
            var tree = new GameObject(birch ? "Abedul_Escarchado" : "Pino_Nevado");
            tree.transform.SetParent(parent, false);
            tree.transform.position = position;
            int size = Mathf.Clamp(Mathf.RoundToInt(height * 2f), 5, 17);
            _landscape.PlaceShared(tree.transform, birch ? $"FrostedBirch_{size}" : $"FrostedPine_{size}", () =>
            {
                var mesh = new Batch();
                if (birch) AddFrostBirch(mesh, size * 0.5f);
                else AddFrostedPine(mesh, Vector3.down * 0.07f, size * 0.5f, marked: false);
                return mesh;
            }, collision: true);
            var node = tree.AddComponent<HarvestableNode>();
            node.SceneryId = sceneryId;
            node.InitializeRuntime(_nodeConfigs["pine"]);
            return node;
        }

        private void AddFrostBirch(Batch tree, float height)
        {
            Material bark = _landscape.Finish(new Color32(179, 192, 201, 255));
            Material scars = _landscape.Finish(new Color32(82, 114, 132, 255));
            Material ice = _landscape.Finish(new Color32(172, 211, 226, 255));
            Vector3 crown = new Vector3(0.12f, height * 0.91f, 0.12f);
            tree.Frustum(Vector3.down * 0.09f, crown, height * 0.042f, 0.04f, 6, bark);
            for (int fork = 0; fork < 5; fork++)
            {
                float angle = fork * 2.39f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 trunk = Vector3.up * (height * (0.43f + fork * 0.085f));
                Vector3 tip = trunk + direction * (height * (0.20f - fork * 0.024f)) + Vector3.up * height * 0.19f;
                tree.Frustum(trunk, tip, 0.10f, 0.022f, 5, fork % 2 == 0 ? bark : scars);
                tree.Ellipsoid(tip, new Vector3(0.72f, 0.47f, 0.66f) * (height * 0.16f), 6, 4, ice);
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
                    Vector3 foot = SurfacePoint(side * (15.8f + i * 2.1f), -15.4f - i % 2 * 3.1f);
                    SpawnSceneryPine(village, foot, 3.7f + 0.42f * (i % 3), $"plaza:{side}:{i}");
                }
            }
            _landscape.Place(village, "ForgeApproach_TracksAndLanterns", plaza, collision: true);
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
            AddKilnFace(assembly, new Vector3(0f, 2.07f, -3.15f), 0.95f, new FoundryMaterials(_landscape));
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
            owner.Place(tree, "CandyTree_BranchesAndCrystals", canopy, collision: true);
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
            _landscape.Place(parent, "Graveyard_FrozenShelters", ruins, collision: true);
        }
    }
}
