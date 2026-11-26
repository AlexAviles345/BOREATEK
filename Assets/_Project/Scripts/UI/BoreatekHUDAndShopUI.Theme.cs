using UnityEngine;
using Boreatek.Audio;
using UnityEngine.UI;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private static readonly Color ColDeepSteel = new Color32(8, 20, 31, 245);
        private static readonly Color ColBlueSteel = new Color32(25, 46, 62, 255);
        private static readonly Color ColSteelRim = new Color32(106, 154, 179, 175);
        private static readonly Color ColBrassRim = new Color32(255, 205, 132, 255);

        private Sprite _metalFace;
        private Sprite _metalRim;
        private Texture2D _titleShadeTexture;

        private void EnsureMetalSprites()
        {
            if (_metalFace == null)
            {
                _metalFace = CreateMetalSprite(rimOnly: false);
                _metalRim = CreateMetalSprite(rimOnly: true);
            }
        }

        private static Sprite CreateMetalSprite(bool rimOnly)
        {
            const int size = 40;
            const float bevel = 9f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                    float dy = Mathf.Min(y + 0.5f, size - y - 0.5f);
                    float edge = Mathf.Min(Mathf.Min(dx, dy), (dx + dy - bevel) * 0.7071f);
                    float alpha = Mathf.Clamp01(edge + 0.5f);
                    if (rimOnly)
                    {
                        alpha *= Mathf.Clamp01(2.3f - edge);
                    }
                    byte shade = rimOnly || y >= size - 3 ? (byte)255 : y < 4 ? (byte)185 : (byte)232;
                    pixels[y * size + x] = new Color32(shade, shade, shade, (byte)(255f * alpha));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, Vector4.one * 11f);
        }

        private Image AddMetalRim(RectTransform parent, Color color)
        {
            RectTransform rect = (RectTransform)CreateUIObject("MetalRim", parent).transform;
            StretchFull(rect);
            Image rim = rect.gameObject.AddComponent<Image>();
            rim.sprite = _metalRim;
            rim.type = Image.Type.Sliced;
            rim.color = color;
            rim.raycastTarget = false;
            return rim;
        }

        private void ApplyMetalPanel(RectTransform panel, Color fill, Color rim)
        {
            EnsureMetalSprites();
            var image = panel.GetComponent<Image>();
            image.sprite = _metalFace;
            image.color = fill;
            AddMetalRim(panel, rim);
        }

        private BoreatekUiButton AttachMetalButton(RectTransform rect, Color surface, Color accent, bool lightSurface = false)
        {
            EnsureMetalSprites();
            Image face = rect.GetComponent<Image>();
            face.sprite = _metalFace;
            Image rim = AddMetalRim(rect, accent);
            Image marker = CreateAccentLine(rect, "FocusMarker", new Vector2(6f, 0f), new Vector2(3f, 18f), accent);
            RectTransform markerRect = marker.rectTransform;
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0f, 0.5f);
            markerRect.pivot = new Vector2(0f, 0.5f);
            markerRect.anchoredPosition = new Vector2(7f, 0f);
            var button = rect.gameObject.AddComponent<BoreatekUiButton>();
            button.Configure(face, rim, marker, surface, accent, lightSurface);
            button.onClick.AddListener(() => BoreatekSoundscape.Play(SoundCue.Click));
            return button;
        }

        private static Image CreateAccentLine(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = (RectTransform)CreateUIObject(name, parent).transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image line = rect.gameObject.AddComponent<Image>();
            line.color = color;
            line.raycastTarget = false;
            return line;
        }

        private static void ConfigureHudScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        private ScrollRect CreateVerticalCatalog(RectTransform parent, string name, string contentName,
            Vector2 position, Vector2 size)
        {
            var topLeft = new Vector2(0f, 1f);
            RectTransform viewport = CreatePanelCard(name, parent, topLeft, topLeft, topLeft,
                position, size, new Color32(9, 19, 29, 255));
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = (RectTransform)CreateUIObject(contentName, viewport).transform;
            content.anchorMin = content.anchorMax = content.pivot = topLeft;
            content.sizeDelta = size;

            RectTransform rail = CreatePanelCard(name + "_ScrollRail", parent, topLeft, topLeft, topLeft,
                position + new Vector2(size.x + 6f, 0f), new Vector2(16f, size.y), ColBlueSteel);
            RectTransform area = (RectTransform)CreateUIObject("SlidingArea", rail).transform;
            StretchFull(area, 3f);
            RectTransform handle = (RectTransform)CreateUIObject("Handle", area).transform;
            StretchFull(handle);
            Image grip = handle.gameObject.AddComponent<Image>();
            grip.color = ColIceCyan;
            Scrollbar scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = grip;
            scrollbar.handleRect = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private static void RevealCatalogItem(ScrollRect scroll, RectTransform item)
        {
            Rect view = scroll.viewport.rect;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, item);
            float distance = bounds.min.y < view.yMin ? view.yMin - bounds.min.y : Mathf.Min(0f, view.yMax - bounds.max.y);
            if (Mathf.Approximately(distance, 0f))
            {
                return;
            }

            scroll.StopMovement();
            Vector2 position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + distance, 0f, Mathf.Max(0f, scroll.content.rect.height - view.height));
            scroll.content.anchoredPosition = position;
        }

        private RawImage CreateTitleShade(RectTransform root)
        {
            const int width = 64;
            _titleShadeTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width];
            for (int i = 0; i < width; i++)
            {
                float x = i / (width - 1f);
                pixels[i] = new Color(0.015f, 0.037f, 0.065f, 0.82f * (1f - Mathf.SmoothStep(0f, 1f, x / 0.77f)));
            }
            _titleShadeTexture.SetPixels(pixels);
            _titleShadeTexture.Apply(false, true);
            RawImage shade = CreateUIObject("TitleShade", root).AddComponent<RawImage>();
            shade.texture = _titleShadeTexture;
            shade.raycastTarget = false;
            StretchFull(shade.rectTransform);
            return shade;
        }

        private void ReleaseThemeGraphics()
        {
            ReleaseGeneratedSprite(_metalFace);
            ReleaseGeneratedSprite(_metalRim);
            ReleaseGeneratedSprite(_roundedCardSprite);
            ReleaseGeneratedSprite(_flatBarSprite);
            ReleaseGeneratedGraphic(_titleShadeTexture);
            ReleaseGeneratedGraphic(_frostVignetteTexture);
        }

        private static void ReleaseGeneratedSprite(Sprite sprite)
        {
            if (sprite != null)
            {
                ReleaseGeneratedGraphic(sprite.texture);
                ReleaseGeneratedGraphic(sprite);
            }
        }

        private static void ReleaseGeneratedGraphic(Object graphic)
        {
            if (graphic == null)
            {
                return;
            }

            if (Application.isPlaying) Destroy(graphic);
            else DestroyImmediate(graphic);
        }
    }
}
