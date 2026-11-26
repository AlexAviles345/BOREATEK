using Boreatek.Core;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private static readonly (BiomeType biome, Color tint)[] NorthernLandscapes =
        {
            (BiomeType.GlacialFlats, new Color32(119, 181, 207, 255)),
            (BiomeType.SnowDunes, new Color32(195, 220, 236, 255)),
            (BiomeType.SnowyTaiga, new Color32(133, 174, 177, 255)),
            (BiomeType.SleighGraveyard, new Color32(137, 157, 178, 255)),
            (BiomeType.GlacialMoraine, new Color32(144, 160, 174, 255)),
            (BiomeType.FrostBirchForest, new Color32(177, 204, 205, 255)),
            (BiomeType.GiantCandyForest, new Color32(187, 163, 198, 255)),
            (BiomeType.FrostValley, new Color32(150, 178, 198, 255)),
            (BiomeType.AlpineRidges, new Color32(127, 148, 173, 255))
        };

        private readonly struct LandscapeBlend
        {
            private readonly int _first;
            private readonly Vector4 _weights;

            internal LandscapeBlend(int first, float x, float y)
            {
                _first = first;
                _weights = new Vector4((1f - x) * (1f - y), x * (1f - y), (1f - x) * y, x * y);
            }

            internal BiomeType Sample(float value)
            {
                if ((value -= _weights.x) < 0f) return NorthernLandscapes[_first].biome;
                if ((value -= _weights.y) < 0f) return NorthernLandscapes[_first + 1].biome;
                return value < _weights.z ? NorthernLandscapes[_first + 3].biome : NorthernLandscapes[_first + 4].biome;
            }

            internal BiomeType Dominant()
            {
                if (_weights.x >= _weights.y && _weights.x >= _weights.z && _weights.x >= _weights.w)
                    return NorthernLandscapes[_first].biome;
                if (_weights.y >= _weights.z && _weights.y >= _weights.w)
                    return NorthernLandscapes[_first + 1].biome;
                return _weights.z >= _weights.w ? NorthernLandscapes[_first + 3].biome : NorthernLandscapes[_first + 4].biome;
            }

            internal Color Color => NorthernLandscapes[_first].tint * _weights.x + NorthernLandscapes[_first + 1].tint * _weights.y
                + NorthernLandscapes[_first + 3].tint * _weights.z + NorthernLandscapes[_first + 4].tint * _weights.w;
        }

        private LandscapeBlend NorthernLandscapeAt(float x, float z)
        {
            float nx = x + _noiseOffset.x, nz = z + _noiseOffset.y;
            float humidity = Mathf.PerlinNoise(nx * 0.0054f + 145f, nz * 0.0054f + 26f);
            float lift = Mathf.PerlinNoise(nx * 0.0064f, nz * 0.0064f);
            float terrainGrain = (Mathf.PerlinNoise(nx * 0.034f + 7f, nz * 0.034f + 11f) - 0.5f) * 0.12f;
            float u = Mathf.InverseLerp(0.26f, 0.73f, humidity + terrainGrain) * 2f;
            float v = Mathf.InverseLerp(0.27f, 0.72f, lift - terrainGrain * 0.55f) * 2f;
            int column = Mathf.Min(1, (int)u), row = Mathf.Min(1, (int)v);
            return new LandscapeBlend(row * 3 + column, Mathf.SmoothStep(0f, 1f, u - column),
                Mathf.SmoothStep(0f, 1f, v - row));
        }

        internal BiomeType FrontierBiome(float x, float z) => NorthernLandscapeAt(x, z).Dominant();

        internal Color SnowColor(float x, float z)
        {
            Color starter = BoreatekWorldBuilder.SnowTint(BoreatekWorldBuilder.StarterSnowWeights(x, z), x, z);
            float transition = NorthernBlend(x, z);
            if (transition <= 0f) return starter;
            float dust = Mathf.PerlinNoise(x * 0.113f + 32f, z * 0.121f + 42f) * 0.47f;
            Color northern = Color.Lerp(NorthernLandscapeAt(x, z).Color, new Color32(209, 230, 243, 255), dust);
            return Color.Lerp(starter, northern, transition);
        }

        private static float NorthernBlend(float x, float z) =>
            BoreatekWorldBuilder.EaseSnow(0f, 48f, Mathf.Max(Mathf.Abs(x) - 124f, Mathf.Abs(z) - 126f));

        private BiomeType DressingBiome(float x, float z, float sample)
        {
            float transition = NorthernBlend(x, z);
            if (sample < transition) return NorthernLandscapeAt(x, z).Sample(sample / transition);
            return BoreatekWorldBuilder.SampleSnowBiome(BoreatekWorldBuilder.StarterSnowWeights(x, z),
                (sample - transition) / Mathf.Max(0.001f, 1f - transition));
        }

        private static bool CanGrowTree(BiomeType biome, int slot) => biome switch
        {
            BiomeType.SnowyTaiga or BiomeType.FrostBirchForest => slot % 5 != 0,
            BiomeType.FrostValley => slot % 3 != 0,
            BiomeType.OuterTundra => slot % 3 != 0,
            BiomeType.AlpineRidges or BiomeType.SnowDunes => slot % 6 == 0,
            _ => false
        };

        private void AddNorthernFormation(Batch features, Vector3 foot, BiomeType biome, float scale, int slot)
        {
            switch (biome)
            {
                case BiomeType.GlacialFlats:
                    features.Frustum(foot, foot + new Vector3(0.72f, 0.21f, 0.22f), 2.2f + scale, 1.5f + scale, 7,
                        _sharedMaterials.Finish(new Color32(93, 161, 196, 255), smoothness: 0.68f));
                    break;
                case BiomeType.SnowDunes:
                    features.Frustum(foot - Vector3.up * 0.18f, foot + new Vector3(0.77f, 0.57f + scale * 0.45f, 0.14f),
                        2.9f + scale, 0.33f + scale * 0.7f, 8, _sharedMaterials.Finish(new Color32(214, 233, 243, 255)));
                    break;
                case BiomeType.AlpineRidges:
                    Vector3 top = foot + new Vector3(0.76f, 2.9f + scale * 1.8f, 0.4f);
                    features.Frustum(foot - Vector3.up * 0.30f, top, 1.9f + scale * 0.8f, 0.7f, 6, _rock);
                    features.Frustum(top, top + Vector3.up * 0.48f, 0.70f, 0.16f, 6,
                        _sharedMaterials.Finish(new Color32(208, 228, 240, 255)));
                    break;
                case BiomeType.GlacialMoraine:
                    features.Frustum(foot, foot + new Vector3(0.68f, 0.98f + scale, 0.47f), 1.45f + scale * 0.98f, 0.15f, 6, _rock);
                    features.Frustum(foot + new Vector3(1.12f, -0.10f, -0.57f), foot + new Vector3(1.42f, 0.61f, -0.44f),
                        0.74f, 0.21f, 5, _rock);
                    break;
                case BiomeType.GiantCandyForest:
                    features.Frustum(foot, foot + new Vector3(-0.31f, 1f + scale * 1.35f, 0.44f), 0.42f + scale * 0.34f, 0f, 5,
                        _candyAccent);
                    break;
                default:
                    float sign = slot % 2 == 0 ? 1f : -1f;
                    features.Frustum(foot, foot + new Vector3(sign * 0.3f, 0.45f + scale * 0.7f, 0.25f), 0.83f + scale, 0.25f, 6, _rock);
                    break;
            }
        }
    }
}
