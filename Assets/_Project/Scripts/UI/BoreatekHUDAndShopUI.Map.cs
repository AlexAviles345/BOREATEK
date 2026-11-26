using Boreatek.Core;
using Boreatek.World;
using UnityEngine;
using UnityEngine.UI;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private sealed class MapDrawing
        {
            internal readonly Texture2D Texture;
            internal readonly Color32[] Pixels;
            internal MapDrawing(int width, int height)
            {
                Texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
                };
                Pixels = new Color32[width * height];
            }
        }

        private static readonly Color32 FogCell = new Color32(16, 28, 43, 255);
        private static readonly Color32 GridCell = new Color32(22, 36, 52, 255);
        private static readonly Color32 LandMark = new Color32(246, 210, 143, 255);
        private static readonly Color32 PinMark = new Color32(255, 116, 102, 255);
        private static readonly Color32 ActiveMark = new Color32(255, 244, 230, 255);
        private static readonly Color32 PlayerMark = new Color32(103, 243, 254, 255);
        private static readonly Rect[] VillageRoofs =
        {
            new Rect(-5.9f, 0.55f, 11.8f, 12.65f), new Rect(-12.35f, -0.6f, 6.4f, 7.7f),
            new Rect(5.95f, -0.6f, 6.4f, 7.7f), new Rect(7.45f, 7.3f, 5.2f, 5.4f),
            new Rect(-23f, -7.6f, 6.8f, 6.4f), new Rect(15.9f, -7.7f, 6.2f, 6.2f),
            new Rect(-21.9f, 6.2f, 7f, 6.4f)
        };
        private static readonly Rect[] VillageLanes =
        {
            new Rect(-23f, -9.5f, 45.5f, 2f), new Rect(-16.1f, -8.5f, 2.2f, 18.7f),
            new Rect(13.6f, -8.1f, 2.4f, 16.1f), new Rect(-1.7f, -20f, 3.4f, 17f)
        };
        private BoreatekExplorationMap _atlas;
        private RectTransform _atlasPanel, _miniPanel;
        private MapDrawing _miniMap, _fullMap;
        private Text _layerLabel, _locationLabel, _mapMessage;
        private InputField _pinName;
        private Button _savePin, _deletePin;
        private Vector2 _mapCenter, _targetPoint, _previousMiniCenter;
        private float _mapHeight, _miniUpdateAt, _previousHeading;
        private int _mapLayer, _activePin = -1, _miniVersion = -1, _previousMiniLayer = -1;
        private bool _hasMapTarget;

        private void BuildMapPanels(RectTransform sessionRoot, BoreatekGameSession session, BoreatekExplorationMap map)
        {
            _atlas = map;
            var bottomLeft = Vector2.zero;
            _miniPanel = CreatePanelCard("Minimap", (RectTransform)_hudCanvas.transform, bottomLeft, bottomLeft,
                bottomLeft, new Vector2(19f, 31f), new Vector2(194f, 220f), ColDeepSteel);
            var miniButton = AttachMetalButton(_miniPanel, ColBlueSteel, ColIceCyan);
            miniButton.onClick.AddListener(session.OpenMap);
            _miniMap = new MapDrawing(96, 96);
            AddMapImage(_miniPanel, "LocalMap", new Vector2(12f, -12f), new Vector2(170f, 170f), _miniMap, false);
            CreateLabel(_miniPanel, "MapShortcut", "Mapa  [M] · Alt + clic", new Vector2(5f, -186f),
                new Vector2(184f, 24f), 16, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);

            var center = Vector2.one * 0.5f;
            _atlasPanel = CreatePanelCard("ExplorationMap", sessionRoot, center, center, center,
                Vector2.zero, new Vector2(1030f, 680f), ColDeepSteel);
            ApplyMetalPanel(_atlasPanel, ColDeepSteel, ColSteelRim);
            CreateLabel(_atlasPanel, "Title", "Mapa de expedición", new Vector2(25f, -15f), new Vector2(680f, 54f),
                41, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateMenuButton(_atlasPanel, "CloseMap", "Cerrar", 853f, 15f, 147f, ColDeepSteel, session.CloseMap);
            CreateAccentLine(_atlasPanel, "MapHeaderRule", new Vector2(25f, -77f), new Vector2(975f, 2f), ColBrassRim);

            _fullMap = new MapDrawing(208, 172);
            BoreatekMapImage image = AddMapImage(_atlasPanel, "WorldMap", new Vector2(25f, -96f),
                new Vector2(650f, 537f), _fullMap, true);
            image.MapClick = SelectMapPoint;
            image.Zoom = ZoomMap;
            image.Pan = amount =>
            {
                _mapCenter -= Vector2.Scale(amount, MapSize);
                RefreshAtlas();
            };
            CreateLabel(_atlasPanel, "North", "↑ N", new Vector2(35f, -104f), new Vector2(48f, 28f),
                16, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter).raycastTarget = false;
            CreateLabel(_atlasPanel, "MapControls", "Clic: seleccionar  ·  Rueda: zoom  ·  Arrastre derecho: mover",
                new Vector2(28f, -636f), new Vector2(645f, 27f), 16, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);

            _layerLabel = CreateLabel(_atlasPanel, "LayerLabel", "", new Vector2(703f, -94f), new Vector2(297f, 43f),
                25, FontStyle.Bold, ColIceCyan, TextAnchor.MiddleLeft);
            CreateMenuButton(_atlasPanel, "NextLayer", "Cambiar nivel", 703f, 142f, 297f, ColBlueSteel, NextMapLayer);
            CreateMenuButton(_atlasPanel, "FitMap", "Ver toda la zona", 703f, 206f, 297f, ColDeepSteel, FitAtlas);

            CreateAccentLine(_atlasPanel, "PinRule", new Vector2(703f, -280f), new Vector2(297f, 2f), ColSteelRim);
            _locationLabel = CreateLabel(_atlasPanel, "MapLocation", "", new Vector2(703f, -293f), new Vector2(297f, 73f),
                18, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);

            Vector2 topLeft = new Vector2(0f, 1f);
            RectTransform input = CreatePanelCard("PinName", _atlasPanel, topLeft, topLeft, topLeft,
                new Vector2(703f, -376f), new Vector2(297f, 52f), ColCardInner);
            ApplyMetalPanel(input, ColBlueSteel, ColIceCyan);
            Text value = CreateLabel(input, "Value", "", new Vector2(12f, -6f), new Vector2(273f, 40f),
                19, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            value.supportRichText = false;
            Text placeholder = CreateLabel(input, "Placeholder", "Nombre del punto", new Vector2(12f, -6f),
                new Vector2(273f, 40f), 19, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleLeft);
            _pinName = input.gameObject.AddComponent<InputField>();
            _pinName.targetGraphic = input.GetComponent<Image>();
            _pinName.textComponent = value;
            _pinName.placeholder = placeholder;
            _pinName.characterLimit = BoreatekExplorationMap.NameLimit;
            _pinName.lineType = InputField.LineType.SingleLine;
            _pinName.onValueChanged.AddListener(_ => RefreshPinButtons());

            _savePin = CreateMenuButton(_atlasPanel, "SavePin", "Guardar punto", 703f, 440f, 297f, ColBrassGold, SaveMapPin);
            _deletePin = CreateMenuButton(_atlasPanel, "DeletePin", "Quitar punto", 703f, 504f, 297f, ColCandyRed, DeleteMapPin);
            _mapMessage = CreateLabel(_atlasPanel, "MapMessage", "", new Vector2(703f, -578f), new Vector2(297f, 68f),
                17, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
        }

        private BoreatekMapImage AddMapImage(RectTransform parent, string name, Vector2 offset, Vector2 size,
            MapDrawing drawing, bool interactive)
        {
            var topLeft = new Vector2(0f, 1f);
            var image = CreateUIObject(name, parent).AddComponent<BoreatekMapImage>();
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = topLeft;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            image.texture = drawing.Texture;
            image.raycastTarget = interactive;
            return image;
        }

        private void UpdateMinimap()
        {
            if (_atlas == null || _miniPanel == null) return;
            bool visible = !_player.IsShopMenuOpen;
            _miniPanel.gameObject.SetActive(visible);
            if (!visible || Time.unscaledTime < _miniUpdateAt) return;
            _miniUpdateAt = Time.unscaledTime + 0.15f;
            int layer = _atlas.LayerAt(_player.transform.position);
            Vector2 point = BoreatekExplorationMap.Planar(_player.transform.position);
            float heading = _player.transform.eulerAngles.y;
            if (_miniVersion == _atlas.Version && _previousMiniLayer == layer &&
                (point - _previousMiniCenter).sqrMagnitude < 0.20f && Mathf.Abs(Mathf.DeltaAngle(_previousHeading, heading)) < 5f)
                return;
            _miniVersion = _atlas.Version;
            _previousMiniLayer = layer;
            _previousMiniCenter = point;
            _previousHeading = heading;
            DrawMap(_miniMap, new Rect(point - Vector2.one * 32f, Vector2.one * 64f), layer, false);
        }

        private Vector2 MapSize => new Vector2(_mapHeight * 650f / 537f, _mapHeight);
        private Rect MapWindow => new Rect(_mapCenter - MapSize * 0.5f, MapSize);

        private void FocusExplorationMap()
        {
            _mapLayer = _atlas.LayerAt(_player.transform.position);
            FitAtlas();
        }

        private void FitAtlas()
        {
            Rect bounds = _atlas.BoundsOf(_mapLayer);
            _mapCenter = bounds.center;
            _mapHeight = Mathf.Max(76f, Mathf.Max(bounds.height, bounds.width * 537f / 650f) * 1.16f);
            ClearPinTarget();
            RefreshAtlas();
        }

        private void NextMapLayer()
        {
            for (int offset = 1; offset < BoreatekExplorationMap.LayerCount; offset++)
            {
                int candidate = (_mapLayer + offset) % BoreatekExplorationMap.LayerCount;
                if (!_atlas.HasLayer(candidate)) continue;
                _mapLayer = candidate;
                FitAtlas();
                return;
            }
            _mapMessage.text = "Explora el subsuelo para descubrir más niveles.";
        }

        private void ZoomMap(float amount)
        {
            if (Mathf.Approximately(amount, 0f)) return;
            Rect bounds = _atlas.BoundsOf(_mapLayer);
            float maximum = Mathf.Max(140f, Mathf.Max(bounds.width, bounds.height) * 1.8f);
            _mapHeight = Mathf.Clamp(_mapHeight * (amount > 0f ? 0.80f : 1.25f), 25f, maximum);
            RefreshAtlas();
        }

        private void SelectMapPoint(Vector2 uv)
        {
            Rect view = MapWindow;
            _targetPoint = view.min + Vector2.Scale(view.size, uv);
            _activePin = -1;
            float nearestDistance = view.height * 0.027f;
            for (int i = 0; i < _atlas.Pins.Count; i++)
            {
                var pin = _atlas.Pins[i];
                if (pin.layer != _mapLayer) continue;
                float distance = Vector2.Distance(pin.point, _targetPoint);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                _activePin = i;
            }
            if (_activePin >= 0) _targetPoint = _atlas.Pins[_activePin].point;
            _hasMapTarget = _atlas.HasCell(_targetPoint, _mapLayer);
            _pinName.SetTextWithoutNotify(_activePin >= 0 ? _atlas.Pins[_activePin].name : "");
            if (_hasMapTarget)
            {
                _locationLabel.text = $"X {_targetPoint.x:0}   Z {_targetPoint.y:0}";
                foreach (var landmark in BoreatekExplorationMap.Landmarks)
                {
                    if (landmark.layer != _mapLayer || Vector2.Distance(landmark.point, _targetPoint) > 4f) continue;
                    _locationLabel.text += "\n" + landmark.name;
                    break;
                }
                _mapMessage.text = "";
            }
            else
            {
                _locationLabel.text = "Zona por explorar";
                _mapMessage.text = "Puedes marcar puntos en zonas descubiertas.";
            }
            RefreshPinButtons();
            RefreshAtlas();
        }

        private void ClearPinTarget()
        {
            _activePin = -1;
            _hasMapTarget = false;
            _pinName.SetTextWithoutNotify("");
            _locationLabel.text = "Selecciona un lugar para marcarlo.";
            _mapMessage.text = "";
            RefreshPinButtons();
        }

        private void RefreshPinButtons()
        {
            _pinName.interactable = _hasMapTarget;
            _savePin.interactable = _hasMapTarget && !string.IsNullOrWhiteSpace(_pinName.text);
            _deletePin.interactable = _activePin >= 0;
        }

        private void SaveMapPin()
        {
            if (!_hasMapTarget || !_atlas.PutPin(_targetPoint, _mapLayer, _pinName.text, _activePin))
            {
                _mapMessage.text = "No se pudo guardar el punto.";
                return;
            }
            if (_activePin < 0) _activePin = _atlas.Pins.Count - 1;
            _mapMessage.text = "Punto guardado en el mapa.";
            RefreshPinButtons();
            RefreshAtlas();
        }

        private void DeleteMapPin()
        {
            if (!_atlas.RemovePin(_activePin)) return;
            ClearPinTarget();
            _mapMessage.text = "Punto eliminado.";
            RefreshAtlas();
        }

        private void RefreshAtlas()
        {
            _layerLabel.text = BoreatekExplorationMap.Layers[_mapLayer];
            DrawMap(_fullMap, MapWindow, _mapLayer, true);
        }

        private void DrawMap(MapDrawing drawing, Rect view, int layer, bool full)
        {
            int width = drawing.Texture.width, height = drawing.Texture.height;
            const float cell = BoreatekExplorationMap.CellSize;
            for (int y = 0; y < height; y++)
            {
                float z = view.yMin + (y + 0.5f) * view.height / height;
                for (int x = 0; x < width; x++)
                {
                    float worldX = view.xMin + (x + 0.5f) * view.width / width;
                    bool known = _atlas.TryReadCell(new Vector2(worldX, z), layer, out Color32 shade);
                    bool grid = Mathf.Repeat(worldX, cell * 2f) < 0.45f || Mathf.Repeat(z, cell * 2f) < 0.45f;
                    if (!known) shade = grid ? GridCell : FogCell;
                    else if (grid) shade = Color32.Lerp(shade, FogCell, 0.13f);
                    drawing.Pixels[y * width + x] = shade;
                }
            }
            if (layer == 0)
            {
                foreach (Rect lane in VillageLanes) PaintMapArea(drawing, view, lane, false);
                foreach (Rect roof in VillageRoofs) PaintMapArea(drawing, view, roof, true);
                foreach (Rect roof in BoreatekWorldBuilder.ExpandedVillageFootprints) PaintMapArea(drawing, view, roof, true);
            }
            foreach (var landmark in BoreatekExplorationMap.Landmarks)
            {
                if (landmark.layer == layer && _atlas.HasCell(landmark.point, layer))
                    StampMap(drawing, view, landmark.point, LandMark, full ? 2 : 1);
            }
            foreach (var pin in _atlas.Pins)
            {
                if (pin.layer == layer) StampMap(drawing, view, pin.point, PinMark, full ? 3 : 2);
            }
            if (full && _hasMapTarget) StampMap(drawing, view, _targetPoint, ActiveMark, 2);
            if (_atlas.LayerAt(_player.transform.position) == layer)
            {
                Vector2 point = BoreatekExplorationMap.Planar(_player.transform.position);
                Vector2 forward = BoreatekExplorationMap.Planar(_player.transform.forward).normalized;
                StampMap(drawing, view, point, PlayerMark, 2);
                StampMap(drawing, view, point + forward * (view.height / height * 4f), PlayerMark, 1);
            }
            drawing.Texture.SetPixels32(drawing.Pixels);
            drawing.Texture.Apply(false, false);
        }

        private void PaintMapArea(MapDrawing drawing, Rect view, Rect area, bool roof)
        {
            if (!view.Overlaps(area)) return;
            int width = drawing.Texture.width, height = drawing.Texture.height;
            int left = Mathf.Max(0, Mathf.FloorToInt((area.xMin - view.xMin) / view.width * width));
            int right = Mathf.Min(width, Mathf.CeilToInt((area.xMax - view.xMin) / view.width * width));
            int bottom = Mathf.Max(0, Mathf.FloorToInt((area.yMin - view.yMin) / view.height * height));
            int top = Mathf.Min(height, Mathf.CeilToInt((area.yMax - view.yMin) / view.height * height));
            Color32 tone = roof ? new Color32(53, 70, 86, 255) : new Color32(101, 119, 134, 255);
            Color32 ridge = new Color32(85, 103, 121, 255);
            for (int y = bottom; y < top; y++)
            {
                for (int x = left; x < right; x++)
                {
                    Vector2 point = view.min + new Vector2((x + 0.5f) / width * view.width, (y + 0.5f) / height * view.height);
                    if (!_atlas.HasCell(point, 0)) continue;
                    drawing.Pixels[y * width + x] = roof && point.y > area.center.y ? ridge : tone;
                }
            }
        }

        private static void StampMap(MapDrawing drawing, Rect view, Vector2 point, Color32 color, int radius)
        {
            if (!view.Contains(point)) return;
            int width = drawing.Texture.width, height = drawing.Texture.height;
            int x = Mathf.FloorToInt((point.x - view.xMin) / view.width * width);
            int y = Mathf.FloorToInt((point.y - view.yMin) / view.height * height);
            for (int dy = -radius - 1; dy <= radius + 1; dy++)
            {
                for (int dx = -radius - 1; dx <= radius + 1; dx++)
                {
                    int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (distance > radius + 1 || x + dx < 0 || x + dx >= width || y + dy < 0 || y + dy >= height) continue;
                    drawing.Pixels[(y + dy) * width + x + dx] = distance <= radius ? color : FogCell;
                }
            }
        }

        private void ReleaseMapGraphics()
        {
            if (_miniMap != null) Destroy(_miniMap.Texture);
            if (_fullMap != null) Destroy(_fullMap.Texture);
        }
    }
}
