using System;
using System.Collections.Generic;
using Boreatek.Core;
using UnityEngine;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private const int LegacyResourceSlots = 4;
        private const int ScenerySlots = 13;
        private const int NodeSlots = LegacyResourceSlots + ScenerySlots + CaveResourceSlots + DeepCaveResourceSlots;
        private readonly Dictionary<Vector2Int, HarvestableNode[]> _frontierNodes = new Dictionary<Vector2Int, HarvestableNode[]>();
        private readonly Dictionary<(Vector2Int sector, int slot), HarvestableNode.SaveState> _nodeChanges =
            new Dictionary<(Vector2Int, int), HarvestableNode.SaveState>();

        [Serializable]
        internal struct FrontierNodeSave
        {
            public int x, z, slot;
            public HarvestableNode.SaveState state;
        }

        private BiomeType LegacyFrontierBiome(float x, float z)
        {
            float climate = FrontierClimate(x, z);
            if (climate > 0.62f) return BiomeType.GiantCandyForest;
            if (climate < 0.36f) return BiomeType.SleighGraveyard;
            return BiomeType.OuterTundra;
        }

        private float FrontierClimate(float x, float z) =>
            Mathf.PerlinNoise((x + _noiseOffset.x) * 0.0068f + 47f, (z + _noiseOffset.y) * 0.0068f + 103f);

        private void BuildFrontierResources(Vector2Int sector, BoreatekLandscapeGeometry owner, Transform root, Vector3 origin)
        {
            var random = LegacyResourceRandom(sector);
            for (int slot = 0; slot < LegacyResourceSlots; slot++)
            {
                Vector3 point = LegacyResourcePoint(origin, slot, random);
                float x = point.x, z = point.z;
                float type = (float)random.NextDouble();
                float yaw = (float)random.NextDouble() * 360f;
                if (!IsFrontier(x, z)) continue;

                BiomeType biome = LegacyFrontierBiome(x, z);
                float radius = biome == BiomeType.GiantCandyForest ? 1.1f : biome == BiomeType.SleighGraveyard || type > 0.85f ? 1.5f : 0.48f;
                float drop = FootingDrop(x, z, radius);
                Vector3 foot = new Vector3(x, GroundHeight(x, z) - Mathf.Min(drop, 0.62f), z);
                HarvestableNode node = _builder.CreateFrontierResource(root, owner, foot, biome, type, yaw);
                if (drop > 0.62f) AddResourceFoundation(root, owner, foot, radius, drop - 0.62f);
                AttachResource(sector, slot, node);
            }
        }

        private void AddResourceFoundation(Transform root, BoreatekLandscapeGeometry owner, Vector3 foot, float radius, float drop)
        {
            var stone = new BoreatekLandscapeGeometry.Batch();
            Vector3 local = foot - root.position;
            stone.Frustum(local - Vector3.up * (drop + 0.22f), local + Vector3.up * 0.12f,
                radius * 1.13f, radius, 8, _rock);
            owner.Place(root, "Afloramiento_Bajo_Recurso", stone, collision: true);
        }

        private System.Random LegacyResourceRandom(Vector2Int sector) =>
            new System.Random(CellSeed(_seed ^ 632779, sector.x, sector.y));

        private static Vector3 LegacyResourcePoint(Vector3 origin, int slot, System.Random random) =>
            new Vector3(origin.x + (slot % 2) * 32f + 5f + (float)random.NextDouble() * 22f, 0f,
                origin.z + (slot / 2) * 32f + 5f + (float)random.NextDouble() * 22f);

        private void AttachResource(Vector2Int sector, int slot, HarvestableNode node)
        {
            if (!_frontierNodes.TryGetValue(sector, out HarvestableNode[] nodes))
            {
                nodes = new HarvestableNode[NodeSlots];
                _frontierNodes.Add(sector, nodes);
            }
            nodes[slot] = node;
            if (_nodeChanges.TryGetValue((sector, slot), out HarvestableNode.SaveState state))
                node.RestoreSave(state);
        }

        private void RememberNodes(Vector2Int sector)
        {
            if (!_frontierNodes.TryGetValue(sector, out HarvestableNode[] nodes)) return;

            for (int slot = 0; slot < nodes.Length; slot++)
            {
                HarvestableNode node = nodes[slot];
                if (node == null) continue;
                HarvestableNode.SaveState state = node.CaptureSave();
                if (state.depleted || state.hitPoints != node.NodeConfig.MaxHitPoints || state.meltSeconds > 0f)
                {
                    _nodeChanges[(sector, slot)] = state;
                }
                else
                {
                    _nodeChanges.Remove((sector, slot));
                }
            }
        }

        internal FrontierNodeSave[] CaptureFrontierNodes()
        {
            foreach (Vector2Int sector in _frontierNodes.Keys) RememberNodes(sector);

            var saved = new FrontierNodeSave[_nodeChanges.Count];
            int i = 0;
            foreach (var item in _nodeChanges)
            {
                saved[i++] = new FrontierNodeSave
                {
                    x = item.Key.sector.x, z = item.Key.sector.y, slot = item.Key.slot, state = item.Value
                };
            }
            return saved;
        }

        internal void RestoreFrontierNodes(FrontierNodeSave[] saved)
        {
            _nodeChanges.Clear();
            if (saved == null) return;

            foreach (FrontierNodeSave item in saved)
            {
                if (item.slot < 0 || item.slot >= NodeSlots) continue;
                var sector = new Vector2Int(item.x, item.z);
                _nodeChanges[(sector, item.slot)] = item.state;
                if (_frontierNodes.TryGetValue(sector, out HarvestableNode[] nodes) && nodes[item.slot] != null)
                {
                    nodes[item.slot].RestoreSave(item.state);
                }
            }
        }
    }
}
