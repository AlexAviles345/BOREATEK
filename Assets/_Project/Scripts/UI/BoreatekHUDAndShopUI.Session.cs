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
        private Text _sessionMessage;
        private Text _gameplayMessage;
        private RawImage _titleArtwork;
        private RawImage _titleShade;
        private RectTransform _gameplayMenuCard;
        private RectTransform _mainMenu, _pauseMenu, _deathMenu;
        private Button _playButton, _resumeButton, _respawnButton;

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
            ConfigureHudScaler(scaler);
            _sessionCanvas.AddComponent<GraphicRaycaster>();

            _sessionBackdrop = _sessionCanvas.AddComponent<Image>();
            RectTransform root = (RectTransform)_sessionCanvas.transform;
            BuildTitleScreen(root, session);
            BuildSaveScreens(root, session);

            var center = new Vector2(0.5f, 0.5f);
            _gameplayMenuCard = CreatePanelCard("SessionCard", root,
                center, center, center, Vector2.zero, new Vector2(560f, 416f), ColPanelDark);
            ApplyMetalPanel(_gameplayMenuCard, ColDeepSteel, ColSteelRim);

            _pauseMenu = CreateSessionPanel(_gameplayMenuCard, "PauseMenu", "En pausa", ColIceCyan);
            _resumeButton = CreateSessionButton(_pauseMenu, "Resume", "Reanudar", 165f, ColBrassGold, session.Resume);
            CreateSessionButton(_pauseMenu, "SaveGame", "Guardar partida", 231f, ColBlueSteel, session.SaveGame);
            CreateSessionButton(_pauseMenu, "MainMenu", "Volver al menú principal", 297f, ColDeepSteel, session.ReturnToMainMenu);

            _deathMenu = CreateSessionPanel(_gameplayMenuCard, "DeathMenu", "Has muerto", ColCandyRed);
            _respawnButton = CreateSessionButton(_deathMenu, "Respawn", "Reaparecer en la aldea", 182f, ColBrassGold, session.Respawn);
            CreateSessionButton(_deathMenu, "MainMenu", "Volver al menú principal", 248f, ColDeepSteel, session.ReturnToMainMenu);

            _gameplayMessage = CreateLabel(_gameplayMenuCard, "SessionMessage", "",
                new Vector2(28f, -362f), new Vector2(504f, 32f), 18,
                FontStyle.Normal, ColTextMuted, TextAnchor.MiddleCenter);
        }

        private void BuildTitleScreen(RectTransform root, BoreatekGameSession session)
        {
            _titleArtwork = CreateUIObject("TitleArtwork", root).AddComponent<RawImage>();
            _titleArtwork.texture = Resources.Load<Texture2D>("Boreatek/TitleBackdrop");
            _titleArtwork.raycastTarget = false;
            StretchFull(_titleArtwork.rectTransform);
            if (_titleArtwork.texture != null)
            {
                var aspect = _titleArtwork.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspect.aspectRatio = (float)_titleArtwork.texture.width / _titleArtwork.texture.height;
            }
            _titleShade = CreateTitleShade(root);

            _mainMenu = (RectTransform)CreateUIObject("MainMenu", root).transform;
            _mainMenu.anchorMin = _mainMenu.anchorMax = new Vector2(0.085f, 0.53f);
            _mainMenu.pivot = new Vector2(0f, 0.5f);
            _mainMenu.sizeDelta = new Vector2(635f, 310f);
            CreateAccentLine(_mainMenu, "BrassRule", new Vector2(7f, -12f), new Vector2(54f, 3f), ColBrassRim);
            CreateAccentLine(_mainMenu, "FrostRule", new Vector2(65f, -12f), new Vector2(258f, 1f), ColSteelRim);
            CreateLabel(_mainMenu, "Title", "BOREATEK", new Vector2(1f, -27f), new Vector2(630f, 110f), 94,
                FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateLabel(_mainMenu, "Subtitle", "Supervivencia en la frontera polar", new Vector2(7f, -139f),
                new Vector2(615f, 36f), 27, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);
            _playButton = CreateMenuButton(_mainMenu, "Play", "Jugar   ›", 7f, 211f, 286f, ColBrassGold, session.OpenSaveSelection);
        }

        internal void ShowSessionScreen(SessionScreen screen, string message)
        {
            bool menuOpen = screen != SessionScreen.Playing;
            bool titleOpen = screen == SessionScreen.MainMenu || screen == SessionScreen.SaveSelection ||
                screen == SessionScreen.CreateSave;
            _hudCanvas.enabled = !menuOpen;
            _hudCanvas.GetComponent<GraphicRaycaster>().enabled = !menuOpen;
            _sessionCanvas.SetActive(menuOpen);
            _titleArtwork.enabled = titleOpen && _titleArtwork.texture != null;
            _titleShade.enabled = _titleArtwork.enabled;
            _mainMenu.gameObject.SetActive(screen == SessionScreen.MainMenu);
            _saveMenu.gameObject.SetActive(screen == SessionScreen.SaveSelection);
            _createSaveMenu.gameObject.SetActive(screen == SessionScreen.CreateSave);
            _gameplayMenuCard.gameObject.SetActive(screen == SessionScreen.Paused || screen == SessionScreen.Dead);
            _pauseMenu.gameObject.SetActive(screen == SessionScreen.Paused);
            _deathMenu.gameObject.SetActive(screen == SessionScreen.Dead);
            _sessionBackdrop.color = screen switch
            {
                SessionScreen.Dead => new Color(0.13f, 0.025f, 0.04f, 0.83f),
                _ when titleOpen => new Color(0.025f, 0.05f, 0.08f, 1f),
                _ => new Color(0.025f, 0.05f, 0.08f, 0.76f)
            };
            _sessionMessage = screen switch
            {
                SessionScreen.SaveSelection => _saveListMessage,
                SessionScreen.CreateSave => _createSaveMessage,
                _ => _gameplayMessage
            };
            SetSessionMessage(message);

            if (screen == SessionScreen.CreateSave)
            {
                _saveNameField.SetTextWithoutNotify("Nueva expedición");
                _saveNameField.Select();
                _saveNameField.ActivateInputField();
                return;
            }

            Button selected = screen switch
            {
                SessionScreen.MainMenu => _playButton,
                SessionScreen.SaveSelection => _loadSelectedButton.interactable ? _loadSelectedButton : _createSaveButton,
                SessionScreen.Paused => _resumeButton,
                SessionScreen.Dead => _respawnButton,
                _ => null
            };
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selected != null ? selected.gameObject : null);
            }
        }

        internal void SetSessionMessage(string message)
        {
            _sessionMessage.text = message;
        }

        private void OnDestroy()
        {
            if (_sessionCanvas != null)
            {
                Destroy(_sessionCanvas);
            }
            ReleaseThemeGraphics();
        }

        private RectTransform CreateSessionPanel(RectTransform card, string name, string title, Color titleColor)
        {
            RectTransform panel = (RectTransform)CreateUIObject(name, card).transform;
            StretchFull(panel);
            CreateLabel(panel, "Title", title, new Vector2(28f, -60f), new Vector2(504f, 75f), 56,
                FontStyle.Bold, titleColor, TextAnchor.MiddleCenter);
            CreateAccentLine(panel, "HeaderRule", new Vector2(60f, -144f), new Vector2(440f, 2f), titleColor);
            return panel;
        }

        private Button CreateSessionButton(RectTransform panel, string name, string label, float y, Color color,
            UnityAction action) => CreateMenuButton(panel, name, label, 60f, y, 440f, color, action);

        private Button CreateMenuButton(RectTransform panel, string name, string label, float x, float y,
            float width, Color color, UnityAction action)
        {
            Button button = CreateStylizedButton(panel, name, label, new Vector2(x, -y),
                new Vector2(width, 54f), color, action, out Text text);
            text.fontSize = 23;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 23;
            return button;
        }
    }
}
