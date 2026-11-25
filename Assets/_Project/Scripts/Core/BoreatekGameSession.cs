using System;
using Boreatek.AI;
using Boreatek.Automation;
using Boreatek.Economy;
using Boreatek.Player;
using Boreatek.UI;
using Boreatek.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Boreatek.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed partial class BoreatekGameSession : MonoBehaviour
    {
        internal enum Screen { MainMenu, SaveSelection, CreateSave, Playing, Paused, Dead }

        private PlayerController _player;
        private TemperatureSystem _temperature;
        private InventoryAndShop _inventory;
        private BoreatekHUDAndShopUI _ui;
        private HarvestableNode[] _nodes;
        private DefectiveToyAI[] _enemies;
        private int _worldSeed;
        private Screen _screen;
        private float _gameplayTimeScale;
        private bool _initialized;
        private bool _leavingScene;

        internal void Initialize(PlayerController player, BoreatekHUDAndShopUI ui, Transform worldRoot, int worldSeed)
        {
            if (_temperature != null)
            {
                _temperature.OnDied -= ShowDeath;
            }
            if (!_initialized)
            {
                _gameplayTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            }

            _player = player;
            _temperature = player.GetComponent<TemperatureSystem>();
            _inventory = player.GetComponent<InventoryAndShop>();
            _ui = ui;
            _worldSeed = worldSeed;
            // The seeded world builder creates nodes and enemies in a deterministic hierarchy order.
            _nodes = worldRoot.GetComponentsInChildren<HarvestableNode>(true);
            _enemies = worldRoot.GetComponentsInChildren<DefectiveToyAI>(true);
            _temperature.OnDied += ShowDeath;
            _initialized = true;
            _ui.BuildSessionMenu(this);
            SetScreen(Screen.MainMenu);
        }

        private void Update()
        {
            if (!_initialized || _leavingScene || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            if (_screen == Screen.Playing)
            {
                SetScreen(Screen.Paused);
            }
            else if (_screen == Screen.Paused)
            {
                Resume();
            }
            else if (_screen == Screen.SaveSelection)
            {
                BackToTitle();
            }
            else if (_screen == Screen.CreateSave)
            {
                OpenSaveSelection();
            }
        }

        internal void BackToTitle()
        {
            if (_screen == Screen.SaveSelection)
            {
                SetScreen(Screen.MainMenu);
            }
        }

        internal void OpenSaveSelection()
        {
            if (_screen == Screen.MainMenu || _screen == Screen.CreateSave)
            {
                _ui.RebuildSaveList(ReadSaveEntries(), LoadGame);
                SetScreen(Screen.SaveSelection);
            }
        }

        internal void OpenCreateSave()
        {
            if (_screen == Screen.SaveSelection)
            {
                SetScreen(Screen.CreateSave);
            }
        }

        internal void StartNewGame(string name)
        {
            if (_screen != Screen.CreateSave)
            {
                return;
            }
            name = name.Trim();
            if (name.Length == 0 || name.Length > MaxSaveNameLength)
            {
                _ui.SetSessionMessage($"Escribe un nombre de 1 a {MaxSaveNameLength} caracteres.");
                return;
            }

            string id = Guid.NewGuid().ToString("N");
            if (WriteSave(id, name))
            {
                _activeSaveId = id;
                _activeSaveName = name;
                SetScreen(Screen.Playing);
            }
        }

        internal void LoadGame(string id)
        {
            if (_screen != Screen.SaveSelection)
            {
                return;
            }
            if (!TryReadSave(id, out SaveData save))
            {
                _ui.RebuildSaveList(ReadSaveEntries(), LoadGame);
                SetScreen(Screen.SaveSelection, "No se pudo cargar esta partida.");
                return;
            }

            _activeSaveId = id;
            _activeSaveName = GetSaveName(save);
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].RestoreSave(save.nodes[i]);
            }
            for (int i = 0; i < _enemies.Length; i++)
            {
                _enemies[i].RestoreSave(save.enemies[i]);
            }
            _inventory.RestoreSave(save.inventory);
            foreach (ExtractorSave item in save.extractors)
            {
                var extractor = MechanicalElfExtractor.CreateAutomatedExtractorOnNode(_nodes[item.nodeIndex], _inventory);
                extractor.SecondsUntilExtraction = item.secondsUntilExtraction;
            }
            DayNightCycleManager.Instance.RestoreSave(save.campaign);
            _temperature.RestoreSave(save.temperature);
            _player.RestoreSave(save.player);
            SetScreen(Screen.Playing);
        }

        internal void Resume()
        {
            if (_screen == Screen.Paused)
            {
                SetScreen(Screen.Playing);
            }
        }

        internal void SaveGame()
        {
            if (_screen == Screen.Paused && WriteSave(_activeSaveId, _activeSaveName))
            {
                _ui.SetSessionMessage("Partida guardada.");
            }
        }

        internal void ReturnToMainMenu()
        {
            if (_leavingScene || (_screen != Screen.Paused && _screen != Screen.Dead))
            {
                return;
            }

            int sceneIndex = gameObject.scene.buildIndex;
            if (sceneIndex < 0)
            {
                _ui.SetSessionMessage("Esta escena no está incluida en la compilación.");
                return;
            }

            _leavingScene = true;
            SceneManager.LoadScene(sceneIndex);
        }

        private void ShowDeath(bool fromCold)
        {
            _player.SetCharacterPresent(false);
            SetScreen(Screen.Dead, fromCold ? "La hipotermia acabó con tu expedición." :
                "Has caído en combate.");
        }

        internal void Respawn()
        {
            if (_screen == Screen.Dead)
            {
                _temperature.RespawnAtVillage();
                _player.SetCharacterPresent(true);
                SetScreen(Screen.Playing);
            }
        }

        private void SetScreen(Screen screen, string message = "")
        {
            _screen = screen;
            bool playing = screen == Screen.Playing;
            Time.timeScale = playing ? _gameplayTimeScale : 0f;
            _player.SetGameplayEnabled(playing);
            _ui.ShowSessionScreen(screen, message);
        }

        private void OnDestroy()
        {
            if (_temperature != null)
            {
                _temperature.OnDied -= ShowDeath;
            }
            if (_initialized)
            {
                Time.timeScale = _gameplayTimeScale;
            }
        }

    }
}
