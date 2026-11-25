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
        private RectTransform _saveMenu, _createSaveMenu, _saveListContent;
        private Text _saveListMessage, _createSaveMessage;
        private ScrollRect _saveListScroll;
        private Button _loadSelectedButton, _createSaveButton;
        private InputField _saveNameField;
        private GameObject _emptySaveList;
        private BoreatekUiButton _selectedSaveRow;
        private string _selectedSaveId;
        private Action<string> _loadSelectedSave;

        private void BuildSaveScreens(RectTransform root, BoreatekGameSession session)
        {
            var anchor = new Vector2(0.085f, 0.5f);
            var pivot = new Vector2(0f, 0.5f);
            _saveMenu = CreatePanelCard("SaveSelection", root, anchor, anchor, pivot, Vector2.zero,
                new Vector2(730f, 610f), ColDeepSteel);
            ApplyMetalPanel(_saveMenu, ColDeepSteel, ColSteelRim);
            CreateLabel(_saveMenu, "Title", "Partidas guardadas", new Vector2(34f, -24f), new Vector2(660f, 70f),
                49, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateAccentLine(_saveMenu, "HeaderRule", new Vector2(34f, -99f), new Vector2(662f, 2f), ColBrassRim);
            BuildSaveList(_saveMenu);

            _saveListMessage = CreateLabel(_saveMenu, "SaveListMessage", "", new Vector2(34f, -487f),
                new Vector2(660f, 35f), 18, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            _loadSelectedButton = CreateMenuButton(_saveMenu, "LoadSelected", "Jugar partida", 34f, 530f, 228f,
                ColBrassGold, () => _loadSelectedSave?.Invoke(_selectedSaveId));
            _createSaveButton = CreateMenuButton(_saveMenu, "CreateNew", "Crear partida", 276f, 530f, 224f,
                ColBlueSteel, session.OpenCreateSave);
            CreateMenuButton(_saveMenu, "Back", "Volver", 514f, 530f, 182f, ColDeepSteel, session.BackToTitle);

            _createSaveMenu = CreatePanelCard("CreateSave", root, anchor, anchor, pivot, Vector2.zero,
                new Vector2(630f, 374f), ColDeepSteel);
            ApplyMetalPanel(_createSaveMenu, ColDeepSteel, ColSteelRim);
            CreateLabel(_createSaveMenu, "Title", "Nueva partida", new Vector2(34f, -25f), new Vector2(560f, 65f),
                49, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateAccentLine(_createSaveMenu, "HeaderRule", new Vector2(34f, -98f), new Vector2(562f, 2f), ColBrassRim);
            CreateLabel(_createSaveMenu, "NameLabel", "Nombre de la partida", new Vector2(34f, -114f),
                new Vector2(560f, 34f), 23, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);

            var topLeft = new Vector2(0f, 1f);
            RectTransform input = CreatePanelCard("SaveName", _createSaveMenu, topLeft, topLeft, topLeft,
                new Vector2(34f, -156f), new Vector2(562f, 60f), ColCardInner);
            ApplyMetalPanel(input, ColBlueSteel, ColIceCyan);
            Text nameText = CreateLabel(input, "InputText", "", new Vector2(16f, -8f), new Vector2(530f, 44f),
                25, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            nameText.supportRichText = false;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _saveNameField = input.gameObject.AddComponent<InputField>();
            _saveNameField.targetGraphic = input.GetComponent<Image>();
            _saveNameField.textComponent = nameText;
            _saveNameField.characterLimit = BoreatekGameSession.MaxSaveNameLength;
            _saveNameField.lineType = InputField.LineType.SingleLine;
            _saveNameField.customCaretColor = true;
            _saveNameField.caretColor = ColIceCyan;
            _saveNameField.selectionColor = new Color(0.27f, 0.54f, 0.74f, 0.60f);

            _createSaveMessage = CreateLabel(_createSaveMenu, "CreateSaveMessage", "", new Vector2(34f, -228f),
                new Vector2(562f, 42f), 18, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            CreateMenuButton(_createSaveMenu, "StartNew", "Crear y jugar", 34f, 287f, 276f, ColBrassGold,
                () => session.StartNewGame(_saveNameField.text));
            CreateMenuButton(_createSaveMenu, "Cancel", "Volver", 324f, 287f, 272f, ColDeepSteel,
                session.OpenSaveSelection);
        }

        private void BuildSaveList(RectTransform panel)
        {
            _saveListScroll = CreateVerticalCatalog(panel, "SaveViewport", "SaveRows",
                new Vector2(34f, -112f), new Vector2(640f, 360f));
            _saveListContent = _saveListScroll.content;
            _emptySaveList = CreateLabel(_saveListScroll.viewport, "EmptyState", "Aún no hay partidas guardadas.",
                new Vector2(20f, -144f), new Vector2(600f, 66f), 24, FontStyle.Normal,
                ColTextMuted, TextAnchor.MiddleCenter).gameObject;
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
                    new Vector2(10f, -10f - i * rowSpacing), new Vector2(620f, rowHeight), ColBlueSteel);
                var rowButton = AttachMetalButton(row, ColBlueSteel, ColIceCyan);
                rowButton.onClick.AddListener(() => SelectSave(entry.id, rowButton));
                rowButton.Focused += () => RevealCatalogItem(_saveListScroll, row);

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
                    SelectSave(entry.id, rowButton);
                }
            }
        }

        private void SelectSave(string id, BoreatekUiButton row)
        {
            if (_selectedSaveRow != null)
            {
                _selectedSaveRow.SetLatched(false);
            }
            _selectedSaveId = id;
            _selectedSaveRow = row;
            _selectedSaveRow.SetLatched(true);
            _loadSelectedButton.interactable = true;
        }
    }
}
