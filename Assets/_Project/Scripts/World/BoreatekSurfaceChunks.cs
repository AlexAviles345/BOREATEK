using System.Collections.Generic;
using UnityEngine;
using Batch = Boreatek.World.BoreatekLandscapeGeometry.Batch;

namespace Boreatek.World
{
    public sealed class BoreatekSurfaceChunks : MonoBehaviour
    {
        internal const int ChunkSize = 64;
        private const int VertexStep = 4;
        private const int GridOffsetZ = -2;
        private const int ViewRadius = 4;
        private const int RetentionRadius = ViewRadius + 1;
        private const int ChunksPerFrame = 2;

        private readonly Dictionary<Vector2Int, Transform> _chunks = new Dictionary<Vector2Int, Transform>();
        private readonly List<Vector2Int> _pending = new List<Vector2Int>();
        private readonly List<Vector2Int> _retired = new List<Vector2Int>();
        private Transform _traveler;
        private BoreatekWorldBuilder _builder;
        private Material[] _snow;
        private Material _rock;
        private int _seed;
        private Vector2 _noiseOffset;
        private Vector2Int _center;
        private int _queueIndex;
        private bool _hasCenter;

        internal void Initialize(Transform traveler, BoreatekWorldBuilder builder, int seed,
            BoreatekLandscapeGeometry materials, Material[] snow)
        {
            _traveler = traveler;
            _builder = builder;
            _seed = seed;
            _snow = snow;
            _rock = materials.Finish(new Color32(81, 112, 133, 255));
            _noiseOffset = new Vector2((CellSeed(seed, 17, 50) & 4095) * 0.27f, (CellSeed(seed, 88, 13) & 4095) * 0.27f);
            EnsureGroundAt(traveler.position);
        }

        internal static Vector2Int SectorAt(Vector3 position) =>
            new Vector2Int(Mathf.FloorToInt(position.x / ChunkSize),
                Mathf.FloorToInt((position.z - GridOffsetZ) / ChunkSize));

        internal static bool IsFrontier(float x, float z) => Mathf.Abs(x) > 124f || Mathf.Abs(z) > 126f;

        internal void EnsureGroundAt(Vector3 position)
        {
            Vector2Int sector = SectorAt(position);
            if (_hasCenter && sector == _center) return;

            _center = sector;
            _hasCenter = true;
            _pending.Clear();
            _queueIndex = 0;
            for (int distance = 0; distance <= ViewRadius; distance++)
            {
                for (int z = -distance; z <= distance; z++)
                {
                    for (int x = -distance; x <= distance; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != distance) continue;
                        Vector2Int wanted = sector + new Vector2Int(x, z);
                        // The landing sector and its neighbors must already collide after loading or respawning.
                        if (distance <= 1) Load(wanted);
                        else if (!_chunks.ContainsKey(wanted)) _pending.Add(wanted);
                    }
                }
            }

            _retired.Clear();
            foreach (Vector2Int existing in _chunks.Keys)
            {
                Vector2Int delta = existing - sector;
                if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) > RetentionRadius) _retired.Add(existing);
            }
            foreach (Vector2Int retired in _retired)
            {
                Transform chunk = _chunks[retired];
                _chunks.Remove(retired);
                chunk.gameObject.SetActive(false);
                Destroy(chunk.gameObject);
            }
        }

        private void Update()
        {
            if (_traveler == null || Time.timeScale <= 0f || _traveler.position.y < -1.8f) return;

            EnsureGroundAt(_traveler.position);
            for (int i = 0; i < ChunksPerFrame && _queueIndex < _pending.Count; i++)
            {
                Load(_pending[_queueIndex++]);
            }
        }

        private void Load(Vector2Int sector)
        {
            if (_chunks.ContainsKey(sector)) return;
            var root = new GameObject($"Surface_{sector.x}_{sector.y}");
            root.transform.SetParent(transform, false);
            Vector3 origin = new Vector3(sector.x * (float)ChunkSize, 0f, sector.y * (float)ChunkSize + GridOffsetZ);
            root.transform.localPosition = origin;
            var owner = root.AddComponent<BoreatekLandscapeGeometry>();
            var snowfield = new Batch();

            for (int z = 0; z < ChunkSize; z += VertexStep)
            {
                for (int x = 0; x < ChunkSize; x += VertexStep)
                {
                    float wx = origin.x + x, wz = origin.z + z;
                    // Keep the original north opening above the first mining ramp.
                    if (wx >= -4f && wx < 4f && wz >= 14f && wz < 34f) continue;
                    var a = new Vector3(x, VertexHeight(wx, wz), z);
                    var b = new Vector3(x + VertexStep, VertexHeight(wx + VertexStep, wz), z);
                    var c = new Vector3(x + VertexStep, VertexHeight(wx + VertexStep, wz + VertexStep), z + VertexStep);
                    var d = new Vector3(x, VertexHeight(wx, wz + VertexStep), z + VertexStep);
                    snowfield.Triangle(a, d, b, _snow[BoreatekWorldBuilder.SnowBand(wx + 1f, wz + 1f)]);
                    snowfield.Triangle(b, d, c, _snow[BoreatekWorldBuilder.SnowBand(wx + 3f, wz + 3f)]);
                }
            }
            owner.Place(root.transform, "ArcticSnowfield", snowfield, collision: true);
            DressFrontier(owner, root.transform, sector, origin);
            _chunks.Add(sector, root.transform);
        }

        private void DressFrontier(BoreatekLandscapeGeometry owner, Transform root, Vector2Int sector, Vector3 origin)
        {
            var random = new System.Random(CellSeed(_seed, sector.x, sector.y));
            var features = new Batch();
            bool hasFeatures = false;
            for (int i = 0; i < 13; i++)
            {
                float x = (float)random.NextDouble() * 60f + 2f;
                float z = (float)random.NextDouble() * 60f + 2f;
                float scale = (float)random.NextDouble();
                float wx = x + origin.x, wz = z + origin.z;
                if (!IsFrontier(wx, wz)) continue;
                var foot = new Vector3(x, GroundHeight(wx, wz) - 0.11f, z);
                if (i % 3 == 0)
                {
                    features.Frustum(foot, foot + new Vector3(0.3f, 0.45f + scale * 0.7f, 0.25f), 0.83f + scale, 0.25f, 6, _rock);
                }
                else
                {
                    _builder.AddFrostedPine(features, foot, 3.4f + scale * 3.5f, marked: false);
                }
                hasFeatures = true;
            }
            if (hasFeatures) owner.Place(root, "WindScouredPinesAndRock", features);
        }

        internal float GroundHeight(float x, float z)
        {
            float tx = Mathf.Floor(x / VertexStep) * VertexStep;
            float tz = Mathf.Floor((z - GridOffsetZ) / VertexStep) * VertexStep + GridOffsetZ;
            float u = (x - tx) / VertexStep, v = (z - tz) / VertexStep;
            float a = VertexHeight(tx, tz), b = VertexHeight(tx + VertexStep, tz);
            float d = VertexHeight(tx, tz + VertexStep);
            if (u + v <= 1f) return a + u * (b - a) + v * (d - a);
            float c = VertexHeight(tx + VertexStep, tz + VertexStep);
            return c + (1f - u) * (d - c) + (1f - v) * (b - c);
        }

        private float VertexHeight(float x, float z)
        {
            Vector3 starter = BoreatekWorldBuilder.SurfacePoint(Mathf.Clamp(x, -124f, 124f), Mathf.Clamp(z, -126f, 126f));
            if (!IsFrontier(x, z)) return starter.y;

            float distanceFromStarter = Mathf.Max(Mathf.Abs(x) - 124f, Mathf.Abs(z) - 126f);
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 48f, distanceFromStarter));
            float nx = x + _noiseOffset.x, nz = z + _noiseOffset.y;
            float plains = Mathf.PerlinNoise(nx * 0.0064f, nz * 0.0064f);
            float ridge = Mathf.PerlinNoise(nx * 0.018f + 42f, nz * 0.018f + 21f);
            float drift = Mathf.PerlinNoise(nx * 0.061f + 11f, nz * 0.058f + 78f);
            float elevation = 1.1f + 12f * plains + 28f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.46f, 0.91f, ridge)) + 1.4f * drift;
            return Mathf.Lerp(starter.y, elevation, fade);
        }

        internal static int CellSeed(int seed, int x, int z)
        {
            unchecked
            {
                uint hash = (uint)seed ^ ((uint)x * 0x9E3779B9u) ^ ((uint)z * 0x85EBCA6Bu);
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (int)hash;
            }
        }
    }
}
