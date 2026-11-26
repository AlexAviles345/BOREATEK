using System;
using System.Collections.Generic;
using Boreatek.Automation;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Economy;
using UnityEngine;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private InventoryAndShop _economy;
        private readonly Dictionary<Vector2Int, List<InventoryAndShop.HeatPlacement>> _siteHeat =
            new Dictionary<Vector2Int, List<InventoryAndShop.HeatPlacement>>();
        private readonly Dictionary<Vector2Int, List<FrontierExtractor>> _siteExtractors =
            new Dictionary<Vector2Int, List<FrontierExtractor>>();
        private readonly HashSet<FrontierExtractor> _remoteExtractors = new HashSet<FrontierExtractor>();
        private double _nextRemoteTick;

        private sealed class FrontierExtractor
        {
            internal int Slot;
            internal ResourceDataSO Resource;
            internal MechanicalElfExtractor Instance;
            internal double NextDelivery;
        }

        [Serializable]
        internal struct FrontierExtractorSave
        {
            public int x, z, slot;
            public ResourceType resource;
            public float secondsUntilExtraction;
        }

        internal void BindEconomy(InventoryAndShop economy) => _economy = economy;

        internal bool RegisterFrontierHeat(InventoryAndShop.HeatPlacement placement)
        {
            Vector3 position = placement.position;
            Vector2Int sector = SectorAt(position);
            if ((position.y < -1.8f || !IsFrontier(position.x, position.z)) && !IsCaveHeatPlacement(position, sector)) return false;
            if (!_siteHeat.TryGetValue(sector, out List<InventoryAndShop.HeatPlacement> sites))
            {
                sites = new List<InventoryAndShop.HeatPlacement>();
                _siteHeat.Add(sector, sites);
            }
            sites.Add(placement);
            if (_chunks.TryGetValue(sector, out Transform root))
            {
                _economy.SpawnHeatSource(placement, root, _sharedMaterials);
            }
            return true;
        }

        internal void UnregisterFrontierHeat(InventoryAndShop.HeatPlacement placement)
        {
            Vector2Int sector = SectorAt(placement.position);
            if (!_siteHeat.TryGetValue(sector, out List<InventoryAndShop.HeatPlacement> sites)) return;
            sites.RemoveAll(item => item.id == placement.id);
            if (sites.Count == 0) _siteHeat.Remove(sector);
        }

        internal bool TryPlaceFrontierExtractor(HarvestableNode node)
        {
            Vector2Int sector = SectorAt(node.transform.position);
            if (!_frontierNodes.TryGetValue(sector, out HarvestableNode[] nodes)) return false;
            int slot = Array.IndexOf(nodes, node);
            if (slot < 0) return false;

            var machine = new FrontierExtractor { Slot = slot, Resource = node.NodeConfig.OutputResource };
            machine.Instance = MechanicalElfExtractor.CreateAutomatedExtractorOnNode(node, _economy,
                _chunks[sector], _sharedMaterials);
            RegisterExtractor(sector, machine);
            return true;
        }

        private void RegisterExtractor(Vector2Int sector, FrontierExtractor machine)
        {
            if (!_siteExtractors.TryGetValue(sector, out List<FrontierExtractor> machines))
            {
                machines = new List<FrontierExtractor>();
                _siteExtractors.Add(sector, machines);
            }
            machines.Add(machine);
        }

        private void SpawnSectorInstallations(Vector2Int sector, Transform root)
        {
            if (_siteHeat.TryGetValue(sector, out List<InventoryAndShop.HeatPlacement> sites))
            {
                foreach (var site in sites) _economy.SpawnHeatSource(site, root, _sharedMaterials);
            }
            if (_siteExtractors.TryGetValue(sector, out List<FrontierExtractor> machines))
            {
                foreach (FrontierExtractor machine in machines) AttachExtractor(sector, root, machine);
            }
        }

        private void AttachExtractor(Vector2Int sector, Transform root, FrontierExtractor machine)
        {
            if (machine.Instance != null || !_frontierNodes.TryGetValue(sector, out HarvestableNode[] nodes) ||
                nodes[machine.Slot] == null) return;

            double now = Time.timeAsDouble;
            DeliverRemoteProduction(machine, now);
            machine.Instance = MechanicalElfExtractor.CreateAutomatedExtractorOnNode(nodes[machine.Slot], _economy,
                root, _sharedMaterials);
            machine.Instance.SecondsUntilExtraction = (float)(machine.NextDelivery - now);
            _remoteExtractors.Remove(machine);
        }

        private void SetExtractorsRemote(Vector2Int sector)
        {
            if (!_siteExtractors.TryGetValue(sector, out List<FrontierExtractor> machines)) return;

            double now = Time.timeAsDouble;
            foreach (FrontierExtractor machine in machines)
            {
                if (machine.Instance == null) continue;
                machine.NextDelivery = now + Mathf.Max(0f, machine.Instance.SecondsUntilExtraction);
                machine.Instance = null;
                _remoteExtractors.Add(machine);
            }
        }

        private void UpdateRemoteExtractors()
        {
            double now = Time.timeAsDouble;
            if (_remoteExtractors.Count == 0 || now < _nextRemoteTick) return;

            _nextRemoteTick = now + 0.5;
            foreach (FrontierExtractor machine in _remoteExtractors) DeliverRemoteProduction(machine, now);
        }

        private void DeliverRemoteProduction(FrontierExtractor machine, double now)
        {
            if (now < machine.NextDelivery) return;

            const float interval = MechanicalElfExtractor.DefaultExtractionInterval;
            int deliveries = (int)((now - machine.NextDelivery) / interval) + 1;
            machine.NextDelivery += deliveries * (double)interval;
            int yield = DayNightCycleManager.Instance != null ? DayNightCycleManager.Instance.ResourceYieldMultiplier : 1;
            _economy.AddAutomatedHopperDelivery(machine.Resource, deliveries * yield);
        }

        internal FrontierExtractorSave[] CaptureFrontierExtractors()
        {
            double now = Time.timeAsDouble;
            var saved = new List<FrontierExtractorSave>();
            foreach (var site in _siteExtractors)
            {
                foreach (FrontierExtractor machine in site.Value)
                {
                    if (machine.Instance == null) DeliverRemoteProduction(machine, now);
                    saved.Add(new FrontierExtractorSave
                    {
                        x = site.Key.x, z = site.Key.y, slot = machine.Slot, resource = machine.Resource.ResourceType,
                        secondsUntilExtraction = machine.Instance != null ? Mathf.Max(0f, machine.Instance.SecondsUntilExtraction) :
                            (float)(machine.NextDelivery - now)
                    });
                }
            }
            return saved.ToArray();
        }

        internal void RestoreFrontierExtractors(FrontierExtractorSave[] saved)
        {
            if (saved == null) return;

            double now = Time.timeAsDouble;
            foreach (FrontierExtractorSave item in saved)
            {
                ResourceDataSO resource = _economy.GetResourceDefinition(item.resource);
                if (resource == null || item.slot < 0 || item.slot >= NodeSlots) continue;
                var sector = new Vector2Int(item.x, item.z);
                float seconds = float.IsFinite(item.secondsUntilExtraction) ?
                    Mathf.Clamp(item.secondsUntilExtraction, 0f, MechanicalElfExtractor.DefaultExtractionInterval) :
                    MechanicalElfExtractor.DefaultExtractionInterval;
                var machine = new FrontierExtractor
                {
                    Slot = item.slot, Resource = resource, NextDelivery = now + seconds
                };
                RegisterExtractor(sector, machine);
                _remoteExtractors.Add(machine);
                if (_chunks.TryGetValue(sector, out Transform root)) AttachExtractor(sector, root, machine);
            }
        }
    }
}
