using Boreatek.Player;
using Boreatek.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Boreatek.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class BoreatekGameSession : MonoBehaviour
    {
        internal enum Screen { MainMenu, Playing, Paused, Dead }

        private PlayerController _player;
        private TemperatureSystem _temperature;
        private BoreatekHUDAndShopUI _ui;
        private Screen _screen;
        private float _gameplayTimeScale;
        private bool _initialized;
        private bool _leavingScene;

        internal void Initialize(PlayerController player, BoreatekHUDAndShopUI ui)
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
            _ui = ui;
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
        }

        internal void StartNewGame()
        {
            if (_screen == Screen.MainMenu)
            {
                SetScreen(Screen.Playing);
            }
        }

        internal void Resume()
        {
            if (_screen == Screen.Paused)
            {
                SetScreen(Screen.Playing);
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
