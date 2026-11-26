using System;
using System.Collections.Generic;
using Boreatek.AI;
using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;
using Species = Boreatek.AI.HostileWildlife.Species;

namespace Boreatek.World
{
    public sealed partial class BoreatekSurfaceChunks
    {
        private const int WildlifeSlots = 10;
        private static readonly (Vector2 point, Species species)[] WildlifeNearVillage =
        {
            (new Vector2(-30f, -70f), Species.SnowWolf),
            (new Vector2(-79f, 33f), Species.SnowWolf),
            (new Vector2(63f, 29f), Species.FrostMoth),
            (new Vector2(74f, -30f), Species.FrostMoth)
        };
        private static readonly (Vector2 point, Species species)[] WildernessEncounters =
        {
            (new Vector2(-53f, -95f), Species.CrystalBeetle),
            (new Vector2(-96f, 48f), Species.BlizzardWraith)
        };
        private readonly Dictionary<Vector2Int, HostileWildlife[]> _wildlife = new();
        private readonly Dictionary<(Vector2Int sector, int slot), float> _wildlifeHealth = new();

        [Serializable]
        internal struct WildlifeSave
        {
            public int x, z, slot;
            public float health;
        }

        private void SpawnSectorWildlife(Vector2Int sector, Transform root, BoreatekLandscapeGeometry owner, Vector3 origin)
        {
            if (!_traveler.TryGetComponent(out PlayerController player)) return;
            var random = new System.Random(CellSeed(_seed ^ 0x6311B, sector.x, sector.y));
            for (int i = 0; i < WildlifeNearVillage.Length; i++)
            {
                var (point, species) = WildlifeNearVillage[i];
                var candidate = new Vector3(point.x, 0f, point.y);
                if (SectorAt(candidate) != sector) continue;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float angle = attempt * 2.39996f;
                    Vector3 trial = candidate + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (attempt * 0.9f);
                    if (TrySpawnSurfaceWildlife(sector, root, owner, i, trial, species, player, random)) break;
                }
            }

            if (random.NextDouble() < 0.31)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 candidate = origin + new Vector3(RandomRange(random, 9f, 55f), 0f, RandomRange(random, 9f, 55f));
                    if (!IsFrontier(candidate.x, candidate.z)) continue;
                    BiomeType biome = DressingBiome(candidate.x, candidate.z, (float)random.NextDouble());
                    Species species = biome is BiomeType.GiantCandyForest or BiomeType.GlacialFlats ? Species.FrostMoth : Species.SnowWolf;
                    if (TrySpawnSurfaceWildlife(sector, root, owner, 4, candidate, species, player, random)) break;
                }
            }
            if (_caveLayouts.TryGetValue(sector, out CaveLayout cave) && random.NextDouble() < 0.66)
            {
                var site = cave.Site;
                Vector3 pocket = site.At(RandomRange(random, -1.2f, 1.2f), site.Length - 2.7f, site.Floor + 1.03f);
                AttachWildlife(sector, 5, root, owner, pocket, Species.FrostMoth, player, RandomRange(random, 0f, 360f));
            }
            SpawnWildernessEncounters(sector, root, owner, origin, player);
        }

        private void SpawnWildernessEncounters(Vector2Int sector, Transform root, BoreatekLandscapeGeometry owner,
            Vector3 origin, PlayerController player)
        {
            // Keep the original species on their existing deterministic coordinates and save slots.
            var random = new System.Random(CellSeed(_seed ^ 0x5B761, sector.x, sector.y));
            for (int i = 0; i < WildernessEncounters.Length; i++)
            {
                var (point, species) = WildernessEncounters[i];
                var candidate = new Vector3(point.x, 0f, point.y);
                if (SectorAt(candidate) != sector) continue;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float angle = attempt * 2.39996f;
                    Vector3 trial = candidate + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * attempt;
                    if (TrySpawnSurfaceWildlife(sector, root, owner, 6 + i, trial, species, player, random)) break;
                }
            }
            if (random.NextDouble() < 0.29)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 candidate = origin + new Vector3(RandomRange(random, 9f, 55f), 0f, RandomRange(random, 9f, 55f));
                    if (!IsFrontier(candidate.x, candidate.z) || NearOtherWildlife(sector, candidate)) continue;
                    BiomeType biome = DressingBiome(candidate.x, candidate.z, (float)random.NextDouble());
                    Species species = biome is BiomeType.GlacialMoraine or BiomeType.GlacialFlats or BiomeType.GiantCandyForest
                        ? Species.CrystalBeetle : Species.BlizzardWraith;
                    if (TrySpawnSurfaceWildlife(sector, root, owner, 8, candidate, species, player, random)) break;
                }
            }
            if (_caveLayouts.TryGetValue(sector, out CaveLayout cave) && random.NextDouble() < 0.65)
            {
                CaveSection a = cave.Sections[^2], b = cave.Sections[^1];
                float t = 0.72f;
                Vector3 pocket = cave.Site.At(Mathf.Lerp(a.Center, b.Center, t) + 0.64f,
                    Mathf.Lerp(a.Along, b.Along, t), Mathf.Lerp(a.Floor, b.Floor, t) + 0.06f);
                if (!NearOtherWildlife(sector, pocket))
                    AttachWildlife(sector, 9, root, owner, pocket, Species.CrystalBeetle, player, RandomRange(random, 0f, 360f));
            }
        }

        private bool NearOtherWildlife(Vector2Int sector, Vector3 position)
        {
            if (!_wildlife.TryGetValue(sector, out HostileWildlife[] creatures)) return false;
            foreach (HostileWildlife creature in creatures)
                if (creature != null && Vector3.ProjectOnPlane(creature.Home - position, Vector3.up).sqrMagnitude < 8f * 8f)
                    return true;
            return false;
        }

        private bool TrySpawnSurfaceWildlife(Vector2Int sector, Transform root, BoreatekLandscapeGeometry owner, int slot,
            Vector3 point, Species species, PlayerController player, System.Random random)
        {
            if (SectorAt(point) != sector || HostileWildlife.InSanctuary(point) ||
                GroundNormal(point.x, point.z).y < 0.82f || OverCampaignMine(point) ||
                (_caveLayouts.TryGetValue(sector, out CaveLayout cave) && cave.Site.BlocksDressingAt(point.x, point.z))) return false;
            if (_frontierNodes.TryGetValue(sector, out HarvestableNode[] nodes))
            {
                foreach (HarvestableNode node in nodes)
                    if (node != null && new Vector2(point.x - node.transform.position.x, point.z - node.transform.position.z).sqrMagnitude < 5.3f)
                        return false;
            }
            point.y = GroundHeight(point.x, point.z) + (species == Species.BlizzardWraith ? 1.25f :
                species == Species.FrostMoth ? 1.03f : 0.06f);
            AttachWildlife(sector, slot, root, owner, point, species, player, RandomRange(random, 0f, 360f));
            return true;
        }

        private void AttachWildlife(Vector2Int sector, int slot, Transform root, BoreatekLandscapeGeometry owner,
            Vector3 point, Species species, PlayerController player, float yaw)
        {
            if (!_wildlife.TryGetValue(sector, out HostileWildlife[] inhabitants))
            {
                inhabitants = new HostileWildlife[WildlifeSlots];
                _wildlife.Add(sector, inhabitants);
            }
            HostileWildlife creature = _builder.SpawnWildlife(root, owner, point, species, player, yaw);
            inhabitants[slot] = creature;
            if (_wildlifeHealth.TryGetValue((sector, slot), out float health)) creature.RestoreHealth(health);
        }

        private void RememberWildlife(Vector2Int sector, HostileWildlife[] inhabitants)
        {
            for (int slot = 0; slot < inhabitants.Length; slot++)
            {
                var creature = inhabitants[slot];
                if (creature != null && creature.CurrentHealth < creature.MaxHealth)
                    _wildlifeHealth[(sector, slot)] = creature.CurrentHealth;
            }
        }

        private void UnloadWildlife(Vector2Int sector)
        {
            if (_wildlife.Remove(sector, out HostileWildlife[] inhabitants)) RememberWildlife(sector, inhabitants);
        }

        internal WildlifeSave[] CaptureWildlife()
        {
            foreach (var sector in _wildlife) RememberWildlife(sector.Key, sector.Value);
            var saved = new WildlifeSave[_wildlifeHealth.Count];
            int index = 0;
            foreach (var change in _wildlifeHealth)
            {
                saved[index++] = new WildlifeSave
                {
                    x = change.Key.sector.x, z = change.Key.sector.y, slot = change.Key.slot, health = change.Value
                };
            }
            return saved;
        }

        internal void RestoreWildlife(WildlifeSave[] saved)
        {
            if (saved == null) return;
            _wildlifeHealth.Clear();
            foreach (WildlifeSave state in saved)
            {
                if (state.slot < 0 || state.slot >= WildlifeSlots || !float.IsFinite(state.health)) continue;
                var sector = new Vector2Int(state.x, state.z);
                _wildlifeHealth[(sector, state.slot)] = state.health;
                if (_wildlife.TryGetValue(sector, out HostileWildlife[] inhabitants) && inhabitants[state.slot] != null)
                    inhabitants[state.slot].RestoreHealth(state.health);
            }
        }
    }
}
