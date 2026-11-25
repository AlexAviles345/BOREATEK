using System;
using System.Collections.Generic;
using Boreatek.Core;
using UnityEngine;
using UnityEngine.UI;
using SaveEntry = Boreatek.Core.BoreatekGameSession.SaveEntry;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private static readonly Color SaveRowColor = new Color(0.09f, 0.18f, 0.28f, 1f);
        private static readonly Color SelectedSaveRowColor = new Color(0.18f, 0.37f, 0.49f, 1f);

        private RectTransform _saveMenu, _createSaveMenu, _saveListContent;
        private Text _saveListMessage, _createSaveMessage;
        private ScrollRect _saveListScroll;
        private Button _loadSelectedButton, _createSaveButton;
        private InputField _saveNameField;
        private GameObject _emptySaveList;
        private Image _selectedSaveRow;
        private string _selectedSaveId;
        private Action<string> _loadSelectedSave;

        private void BuildSaveScreens(RectTransform root, BoreatekGameSession session)
        {
            var anchor = new Vector2(0.085f, 0.5f);
            var pivot = new Vector2(0f, 0.5f);
            var panelColor = new Color(0.035f, 0.075f, 0.12f, 0.97f);
            _saveMenu = CreatePanelCard("SaveSelection", root, anchor, anchor, pivot, Vector2.zero,
                new Vector2(730f, 610f), panelColor);
            CreateLabel(_saveMenu, "Title", "Partidas guardadas", new Vector2(34f, -24f), new Vector2(660f, 70f),
                49, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            BuildSaveList(_saveMenu);

            _saveListMessage = CreateLabel(_saveMenu, "SaveListMessage", "", new Vector2(34f, -487f),
                new Vector2(660f, 35f), 18, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            _loadSelectedButton = CreateMenuButton(_saveMenu, "LoadSelected", "Jugar partida", 34f, 530f, 228f,
                ColBrassGold, () => _loadSelectedSave?.Invoke(_selectedSaveId));
            _createSaveButton = CreateMenuButton(_saveMenu, "CreateNew", "Crear partida", 276f, 530f, 224f,
                ColCardInner, session.OpenCreateSave);
            CreateMenuButton(_saveMenu, "Back", "Volver", 514f, 530f, 182f, ColCardInner, session.BackToTitle);

            _createSaveMenu = CreatePanelCard("CreateSave", root, anchor, anchor, pivot, Vector2.zero,
                new Vector2(630f, 374f), panelColor);
            CreateLabel(_createSaveMenu, "Title", "Nueva partida", new Vector2(34f, -25f), new Vector2(560f, 65f),
                49, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateLabel(_createSaveMenu, "NameLabel", "Nombre de la partida", new Vector2(34f, -114f),
                new Vector2(560f, 34f), 23, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);

            var topLeft = new Vector2(0f, 1f);
            RectTransform input = CreatePanelCard("SaveName", _createSaveMenu, topLeft, topLeft, topLeft,
                new Vector2(34f, -156f), new Vector2(562f, 60f), ColCardInner);
            Text nameText = CreateLabel(input, "InputText", "", new Vector2(16f, -8f), new Vector2(530f, 44f),
                25, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            nameText.supportRichText = false;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _saveNameField = input.gameObject.AddComponent<InputField>();
            _saveNameField.targetGraphic = input.GetComponent<Image>();
            _saveNameField.textComponent = nameText;
            _saveNameField.characterLimit = BoreatekGameSession.MaxSaveNameLength;
            _saveNameField.lineType = InputField.LineType.SingleLine;

            _createSaveMessage = CreateLabel(_createSaveMenu, "CreateSaveMessage", "", new Vector2(34f, -228f),
                new Vector2(562f, 42f), 18, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            CreateMenuButton(_createSaveMenu, "StartNew", "Crear y jugar", 34f, 287f, 276f, ColBrassGold,
                () => session.StartNewGame(_saveNameField.text));
            CreateMenuButton(_createSaveMenu, "Cancel", "Volver", 324f, 287f, 272f, ColCardInner,
                session.OpenSaveSelection);
        }

        private void BuildSaveList(RectTransform panel)
        {
            var topLeft = new Vector2(0f, 1f);
            RectTransform viewport = CreatePanelCard("SaveViewport", panel, topLeft, topLeft, topLeft,
                new Vector2(34f, -112f), new Vector2(640f, 360f), new Color(0.03f, 0.06f, 0.09f, 1f));
            viewport.gameObject.AddComponent<RectMask2D>();
            _saveListContent = (RectTransform)CreateUIObject("SaveRows", viewport).transform;
            _saveListContent.anchorMin = _saveListContent.anchorMax = _saveListContent.pivot = topLeft;
            _saveListContent.sizeDelta = new Vector2(640f, 360f);
            _emptySaveList = CreateLabel(viewport, "EmptyState", "Aún no hay partidas guardadas.",
                new Vector2(20f, -144f), new Vector2(600f, 66f), 24, FontStyle.Normal,
                ColTextMuted, TextAnchor.MiddleCenter).gameObject;

            RectTransform rail = CreatePanelCard("ScrollBar", panel, topLeft, topLeft, topLeft,
                new Vector2(680f, -112f), new Vector2(16f, 360f), ColCardInner);
            RectTransform area = (RectTransform)CreateUIObject("SlidingArea", rail).transform;
            StretchFull(area, 2f);
            RectTransform handle = (RectTransform)CreateUIObject("Handle", area).transform;
            StretchFull(handle);
            Image grip = handle.gameObject.AddComponent<Image>();
            grip.color = ColIceCyan;
            Scrollbar scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = grip;
            scrollbar.handleRect = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            _saveListScroll = viewport.gameObject.AddComponent<ScrollRect>();
            _saveListScroll.viewport = viewport;
            _saveListScroll.content = _saveListContent;
            _saveListScroll.horizontal = false;
            _saveListScroll.movementType = ScrollRect.MovementType.Clamped;
            _saveListScroll.scrollSensitivity = 42f;
            _saveListScroll.verticalScrollbar = scrollbar;
            _saveListScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        internal void RebuildSaveList(IReadOnlyList<SaveEntry> entries, Action<string> loadSave)
        {
            _loadSelectedSave = loadSave;
            _selectedSaveId = null;
            _selectedSaveRow = null;
            _loadSelectedButton.interactable = false;
            foreach (Transform child in _saveListContent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            const float rowHeight = 76f;
            const float rowSpacing = 86f;
            _saveListContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(360f, entries.Count * rowSpacing + 10f));
            _saveListScroll.StopMovement();
            _saveListContent.anchoredPosition = Vector2.zero;
            _emptySaveList.SetActive(entries.Count == 0);

            for (int i = 0; i < entries.Count; i++)
            {
                SaveEntry entry = entries[i];
                var topLeft = new Vector2(0f, 1f);
                RectTransform row = CreatePanelCard("SaveRow_" + entry.id, _saveListContent, topLeft, topLeft, topLeft,
                    new Vector2(10f, -10f - i * rowSpacing), new Vector2(620f, rowHeight), SaveRowColor);
                Image rowImage = row.GetComponent<Image>();
                Button rowButton = row.gameObject.AddComponent<Button>();
                rowButton.targetGraphic = rowImage;
                rowButton.onClick.AddListener(() => SelectSave(entry.id, rowImage));

                Text name = CreateLabel(row, "SaveTitle", entry.name, new Vector2(18f, -9f), new Vector2(580f, 30f),
                    24, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
                name.supportRichText = false;
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 14;
                name.resizeTextMaxSize = 24;
                string details = $"Ciclo {entry.cycle}";
                if (entry.savedAtUtc != DateTime.MinValue)
                {
                    details += $"  ·  {entry.savedAtUtc.ToLocalTime():dd/MM/yyyy HH:mm}";
                }
                CreateLabel(row, "SaveDetails", details, new Vector2(18f, -41f), new Vector2(580f, 25f),
                    18, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);

                if (i == 0)
                {
                    SelectSave(entry.id, rowImage);
                }
            }
        }

        private void SelectSave(string id, Image row)
        {
            if (_selectedSaveRow != null)
            {
                _selectedSaveRow.color = SaveRowColor;
            }
            _selectedSaveId = id;
            _selectedSaveRow = row;
            _selectedSaveRow.color = SelectedSaveRowColor;
            _loadSelectedButton.interactable = true;
        }
    }
}
