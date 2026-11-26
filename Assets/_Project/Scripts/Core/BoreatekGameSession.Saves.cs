using System;
using System.Collections.Generic;
using System.IO;
using Boreatek.AI;
using Boreatek.Automation;
using Boreatek.Economy;
using Boreatek.Player;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.Core
{
    public sealed partial class BoreatekGameSession
    {
        internal const int MaxSaveNameLength = 40;
        private const int SaveVersion = 2;
        private const string LegacySaveKey = "Boreatek.Campaign";
        private const string LegacySaveId = "legacy";

        private string _activeSaveId;
        private string _activeSaveName;
        private string _pendingDeleteId;
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Partidas");

        internal readonly struct SaveEntry
        {
            public readonly string id;
            public readonly string name;
            public readonly int cycle;
            public readonly DateTime savedAtUtc;

            public SaveEntry(string id, string name, int cycle, DateTime savedAtUtc)
            {
                this.id = id;
                this.name = name;
                this.cycle = cycle;
                this.savedAtUtc = savedAtUtc;
            }
        }

        private List<SaveEntry> ReadSaveEntries()
        {
            var entries = new List<SaveEntry>();
            try
            {
                if (Directory.Exists(SaveDirectory))
                {
                    foreach (string path in Directory.EnumerateFiles(SaveDirectory, "*.json"))
                    {
                        string id = Path.GetFileNameWithoutExtension(path);
                        if (Guid.TryParseExact(id, "N", out _) && TryReadSave(id, out SaveData save))
                        {
                            entries.Add(CreateSaveEntry(id, save));
                        }
                    }
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"No se pudieron listar las partidas de BOREATEK: {e.Message}");
            }

            if (TryReadSave(LegacySaveId, out SaveData legacy))
            {
                entries.Add(CreateSaveEntry(LegacySaveId, legacy));
            }

            entries.Sort((left, right) => right.savedAtUtc.CompareTo(left.savedAtUtc));
            return entries;
        }

        private static SaveEntry CreateSaveEntry(string id, SaveData save)
        {
            var savedAt = save.savedAtTicks > 0 && save.savedAtTicks <= DateTime.MaxValue.Ticks
                ? new DateTime(save.savedAtTicks, DateTimeKind.Utc)
                : DateTime.MinValue;
            return new SaveEntry(id, GetSaveName(save), save.campaign.cycle, savedAt);
        }

        private static string GetSaveName(SaveData save) =>
            string.IsNullOrWhiteSpace(save.name) ? "Partida anterior" : save.name;

        internal void OpenDeleteSave(SaveEntry entry)
        {
            if (_screen != Screen.SaveSelection || !IsSaveId(entry.id)) return;
            _pendingDeleteId = entry.id;
            _ui.SetDeleteSaveName(entry.name);
            SetScreen(Screen.ConfirmDeleteSave);
        }

        internal void CancelDeleteSave()
        {
            if (_screen != Screen.ConfirmDeleteSave) return;
            _pendingDeleteId = null;
            SetScreen(Screen.SaveSelection);
        }

        internal void ConfirmDeleteSave()
        {
            if (_screen != Screen.ConfirmDeleteSave || _pendingDeleteId == null) return;
            string id = _pendingDeleteId;
            _pendingDeleteId = null;
            bool deleted = TryDeleteSave(SaveDirectory, id);
            _ui.RebuildSaveList(ReadSaveEntries(), LoadGame);
            SetScreen(Screen.SaveSelection, deleted ? "Partida eliminada." : "No se pudo eliminar la partida.");
        }

        private static bool TryDeleteSave(string directory, string id)
        {
            if (!IsSaveId(id)) return false;
            try
            {
                try
                {
                    File.Delete(Path.Combine(directory, id + ".json"));
                }
                catch (DirectoryNotFoundException)
                {
                    // An already-removed directory has no copy on disk.
                }
                if (id == LegacySaveId && PlayerPrefs.HasKey(LegacySaveKey))
                {
                    PlayerPrefs.DeleteKey(LegacySaveKey);
                    PlayerPrefs.Save();
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlayerPrefsException)
            {
                Debug.LogWarning($"No se pudo eliminar una partida de BOREATEK: {e.Message}");
                return false;
            }
        }

        private static bool IsSaveId(string id) => id == LegacySaveId || Guid.TryParseExact(id, "N", out _);

        private bool TryReadSave(string id, out SaveData save)
        {
            save = null;
            bool isLegacy = id == LegacySaveId;
            if (!IsSaveId(id))
            {
                return false;
            }

            try
            {
                string path = GetSavePath(id);
                if (File.Exists(path) && TryParseSave(File.ReadAllText(path), out save))
                {
                    return true;
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"No se pudo leer una partida de BOREATEK: {e.Message}");
            }

            return isLegacy && TryParseSave(PlayerPrefs.GetString(LegacySaveKey, ""), out save);
        }

        private bool TryParseSave(string json, out SaveData save)
        {
            save = null;
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                save = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return save != null && (save.version == 1 || save.version == SaveVersion) && save.worldSeed == _worldSeed &&
                save.nodes?.Length == _nodes.Length && save.enemies?.Length == _enemies.Length &&
                save.inventory.resources != null && save.inventory.heatSources != null &&
                save.extractors != null && Array.TrueForAll(save.extractors,
                    item => item.nodeIndex >= 0 && item.nodeIndex < _nodes.Length);
        }

        private bool WriteSave(string id, string name)
        {
            var frontierExtractors = _surface.CaptureFrontierExtractors();
            var extractors = new List<ExtractorSave>();
            foreach (var extractor in FindObjectsByType<MechanicalElfExtractor>())
            {
                int nodeIndex = Array.IndexOf(_nodes, extractor.AttachedNode);
                if (nodeIndex >= 0)
                {
                    extractors.Add(new ExtractorSave
                    {
                        nodeIndex = nodeIndex, secondsUntilExtraction = extractor.SecondsUntilExtraction
                    });
                }
            }
            var save = new SaveData
            {
                name = name, savedAtTicks = DateTime.UtcNow.Ticks,
                version = SaveVersion, worldSeed = _worldSeed,
                player = _player.CaptureSave(),
                temperature = _temperature.CaptureSave(),
                inventory = _inventory.CaptureSave(),
                campaign = DayNightCycleManager.Instance.CaptureSave(),
                nodes = Array.ConvertAll(_nodes, node => node.CaptureSave()),
                enemies = Array.ConvertAll(_enemies, enemy => enemy.CaptureSave()),
                frontierNodes = _surface.CaptureFrontierNodes(),
                frontierExtractors = frontierExtractors,
                extractors = extractors.ToArray()
            };

            try
            {
                Directory.CreateDirectory(SaveDirectory);
                string path = GetSavePath(id);
                string pending = path + ".tmp";
                File.WriteAllText(pending, JsonUtility.ToJson(save));
                if (File.Exists(path))
                {
                    File.Replace(pending, path, null);
                }
                else
                {
                    File.Move(pending, path);
                }
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"No se pudo guardar BOREATEK: {e.Message}");
                _ui.SetSessionMessage("No se pudo guardar la partida.");
                return false;
            }
        }

        private static string GetSavePath(string id) => Path.Combine(SaveDirectory, id + ".json");

        [Serializable]
        private class SaveData
        {
            public string name;
            public long savedAtTicks;
            public int version, worldSeed;
            public PlayerController.SaveState player;
            public TemperatureSystem.SaveState temperature;
            public InventoryAndShop.SaveState inventory;
            public DayNightCycleManager.SaveState campaign;
            public HarvestableNode.SaveState[] nodes;
            public DefectiveToyAI.SaveState[] enemies;
            public BoreatekSurfaceChunks.FrontierNodeSave[] frontierNodes;
            public BoreatekSurfaceChunks.FrontierExtractorSave[] frontierExtractors;
            public ExtractorSave[] extractors;
        }

        [Serializable]
        private struct ExtractorSave
        {
            public int nodeIndex;
            public float secondsUntilExtraction;
        }
    }
}
