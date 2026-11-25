using Boreatek.Player;
using Boreatek.UI;
using UnityEngine;

namespace Boreatek.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class BoreatekGameSession : MonoBehaviour
    {
        internal enum Screen { MainMenu, Playing }

        private PlayerController _player;
        private BoreatekHUDAndShopUI _ui;
        private Screen _screen;
        private float _gameplayTimeScale;
        private bool _initialized;

        internal void Initialize(PlayerController player, BoreatekHUDAndShopUI ui)
        {
            if (!_initialized)
            {
                _gameplayTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            }

            _player = player;
            _ui = ui;
            _initialized = true;
            _ui.BuildSessionMenu(this);
            SetScreen(Screen.MainMenu);
        }

        internal void StartNewGame()
        {
            if (_screen == Screen.MainMenu)
            {
                SetScreen(Screen.Playing);
            }
        }

        private void SetScreen(Screen screen)
        {
            _screen = screen;
            bool playing = screen == Screen.Playing;
            Time.timeScale = playing ? _gameplayTimeScale : 0f;
            _player.SetGameplayEnabled(playing);
            _ui.ShowSessionScreen(screen);
        }

        private void OnDestroy()
        {
            if (_initialized)
            {
                Time.timeScale = _gameplayTimeScale;
            }
        }

    }
}
