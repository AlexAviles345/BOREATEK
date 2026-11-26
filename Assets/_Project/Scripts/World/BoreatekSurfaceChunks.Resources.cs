using System;
using System.Collections.Generic;
using Boreatek.Core;
using UnityEngine;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private const int NodeSlots = 4;
        private readonly Dictionary<Vector2Int, HarvestableNode[]> _frontierNodes = new Dictionary<Vector2Int, HarvestableNode[]>();
        private readonly Dictionary<(Vector2Int sector, int slot), HarvestableNode.SaveState> _nodeChanges =
            new Dictionary<(Vector2Int, int), HarvestableNode.SaveState>();

        [Serializable]
        internal struct FrontierNodeSave
        {
            public int x, z, slot;
            public HarvestableNode.SaveState state;
        }

        internal BiomeType FrontierBiome(float x, float z)
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
            var random = new System.Random(CellSeed(_seed ^ 632779, sector.x, sector.y));
            HarvestableNode[] nodes = null;
            for (int slot = 0; slot < NodeSlots; slot++)
            {
                float x = origin.x + (slot % 2) * 32f + 5f + (float)random.NextDouble() * 22f;
                float z = origin.z + (slot / 2) * 32f + 5f + (float)random.NextDouble() * 22f;
                float type = (float)random.NextDouble();
                float yaw = (float)random.NextDouble() * 360f;
                if (!IsFrontier(x, z)) continue;

                nodes ??= new HarvestableNode[NodeSlots];
                Vector3 foot = new Vector3(x, GroundHeight(x, z), z);
                HarvestableNode node = _builder.CreateFrontierResource(root, owner, foot, FrontierBiome(x, z), type, yaw);
                if (_nodeChanges.TryGetValue((sector, slot), out HarvestableNode.SaveState state))
                {
                    node.RestoreSave(state);
                }
                nodes[slot] = node;
            }
            if (nodes != null) _frontierNodes.Add(sector, nodes);
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
