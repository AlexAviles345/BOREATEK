using UnityEngine;
using UnityEngine.UI;

namespace Boreatek.UI
{
    public sealed class BoreatekUiButton : Button
    {
        private static readonly Color Ink = new Color32(25, 18, 11, 255);
        private static readonly Color Light = new Color32(235, 247, 255, 255);
        private static readonly Color DisabledInk = new Color32(177, 190, 200, 255);
        private Image _face;
        private Image _rim;
        private Image _marker;
        private Text _label;
        private Color _surface;
        private Color _accent;
        private bool _lightSurface;
        private bool _latched;

        internal void Configure(Image face, Image rim, Image marker, Color surface, Color accent, bool lightSurface)
        {
            _face = face;
            _rim = rim;
            _marker = marker;
            _surface = surface;
            _accent = accent;
            _lightSurface = lightSurface;
            targetGraphic = face;
            transition = Transition.None;
            DoStateTransition(currentSelectionState, true);
        }

        internal void SetLabel(Text label)
        {
            _label = label;
            DoStateTransition(currentSelectionState, true);
        }

        internal void SetLatched(bool latched)
        {
            _latched = latched;
            DoStateTransition(currentSelectionState, true);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (_face == null)
            {
                return;
            }

            bool disabled = state == SelectionState.Disabled;
            bool pressed = state == SelectionState.Pressed;
            bool focused = state == SelectionState.Selected;
            bool lit = focused || state == SelectionState.Highlighted;
            Color surface = _surface;
            if (disabled)
            {
                surface = new Color32(37, 48, 57, 255);
            }
            else if (pressed)
            {
                surface = Color.Lerp(surface, Color.black, 0.15f);
            }
            else if (lit || _latched)
            {
                surface = Color.Lerp(surface, _lightSurface ? Color.white : _accent, _lightSurface ? 0.20f : 0.22f);
            }

            _face.color = surface;
            Color rimColor = focused ? Light : _accent;
            rimColor.a = disabled ? 0.28f : lit || pressed || _latched ? 1f : 0.52f;
            _rim.color = rimColor;
            _marker.color = focused ? Light : _accent;
            _marker.enabled = !disabled && (focused || _latched);
            if (_label != null)
            {
                _label.color = disabled ? DisabledInk : _lightSurface ? Ink : Light;
            }
        }
    }
}
