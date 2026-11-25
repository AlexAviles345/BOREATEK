using Boreatek.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SessionScreen = Boreatek.Core.BoreatekGameSession.Screen;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private GameObject _sessionCanvas;
        private Image _sessionBackdrop;
        private RectTransform _mainMenu;
        private Button _playButton;

        internal void BuildSessionMenu(BoreatekGameSession session)
        {
            if (_sessionCanvas != null)
            {
                return;
            }

            _sessionCanvas = CreateUIObject("Boreatek_Session_Canvas", null);
            Canvas canvas = _sessionCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = _sessionCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.25f;
            _sessionCanvas.AddComponent<GraphicRaycaster>();

            _sessionBackdrop = _sessionCanvas.AddComponent<Image>();
            RectTransform root = (RectTransform)_sessionCanvas.transform;
            BuildTitleScreen(root, session);
        }

        private void BuildTitleScreen(RectTransform root, BoreatekGameSession session)
        {
            _mainMenu = (RectTransform)CreateUIObject("MainMenu", root).transform;
            _mainMenu.anchorMin = _mainMenu.anchorMax = new Vector2(0.085f, 0.53f);
            _mainMenu.pivot = new Vector2(0f, 0.5f);
            _mainMenu.sizeDelta = new Vector2(635f, 290f);
            CreateLabel(_mainMenu, "Title", "BOREATEK", Vector2.zero, new Vector2(630f, 115f), 96,
                FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateLabel(_mainMenu, "Subtitle", "Supervivencia en la frontera polar", new Vector2(7f, -116f),
                new Vector2(615f, 42f), 29, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            _playButton = CreateMenuButton(_mainMenu, "Play", "Iniciar nueva partida", 7f, 204f, 360f, ColBrassGold, session.StartNewGame);
        }

        internal void ShowSessionScreen(SessionScreen screen)
        {
            bool menuOpen = screen == SessionScreen.MainMenu;
            _hudCanvas.enabled = !menuOpen;
            _hudCanvas.GetComponent<GraphicRaycaster>().enabled = !menuOpen;
            _sessionCanvas.SetActive(menuOpen);
            _sessionBackdrop.color = new Color(0.025f, 0.05f, 0.08f, 1f);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(menuOpen ? _playButton.gameObject : null);
            }
        }

        private void OnDestroy()
        {
            if (_sessionCanvas != null)
            {
                Destroy(_sessionCanvas);
            }
        }

        private Button CreateMenuButton(RectTransform panel, string name, string label, float x, float y,
            float width, Color color, UnityAction action)
        {
            Button button = CreateStylizedButton(panel, name, label, new Vector2(x, -y),
                new Vector2(width, 54f), color, action, out Text text);
            text.fontSize = 23;
            return button;
        }
    }
}
