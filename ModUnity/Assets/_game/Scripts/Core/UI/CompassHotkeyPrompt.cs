using System;
using InControl;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string COMPASS_ICON_PREFAB_NAME = "pfb_ui_key_compas_map";
        public const string COMPASS_PROMPT_NAME      = "InsanityCompassPrompt";
        public const float  COMPASS_PROMPT_POS_X     = 0f;
        public const float  COMPASS_PROMPT_POS_Y     = 238f;
    }

    public class CompassHotkeyPrompt : MonoBehaviour
    {
        private Image _keyImage;
        private Sprite _upSprite;
        private Sprite _downSprite;
        private bool _pressed;
        private Action<BindingSourceType, InputDeviceStyle> _onInputChanged;

        public static GameObject TryCreate()
        {
            if (!G.Prefabs.TryGetValue(COMPASS_ICON_PREFAB_NAME, out var prefab) || prefab == null)
            {
                Log.Warn($"CompassHotkeyPrompt: prefab '{COMPASS_ICON_PREFAB_NAME}' not found among loaded bundles");
                return null;
            }

            if (G.GameCanvas == null)
            {
                Log.Warn("CompassHotkeyPrompt: game canvas not available");
                return null;
            }

            var obj = CloneUiNode(prefab, COMPASS_PROMPT_NAME, G.GameCanvas);

            var rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(COMPASS_PROMPT_POS_X, COMPASS_PROMPT_POS_Y);
            }

            obj.AddComponent<CompassHotkeyPrompt>();

            Log.Info("CompassHotkeyPrompt: created");
            return obj;
        }

        public void Start()
        {
            var view = GetComponentInChildren<KeyPromptView>(true);
            if (view == null)
            {
                Log.Warn("CompassHotkeyPrompt: KeyPromptView not found in prefab");
                return;
            }

            _keyImage = view.Key;
            if (_keyImage == null)
            {
                Log.Warn("CompassHotkeyPrompt: KeyPromptView.Key is not assigned");
                return;
            }

            RefreshIcon();

            _onInputChanged = (source, style) => RefreshIcon();
            SubscribeInputChanged(_onInputChanged);
        }

        public void OnDestroy()
        {
            if (_onInputChanged == null)
                return;

            UnsubscribeInputChanged(_onInputChanged);
            _onInputChanged = null;
        }

        public void Update()
        {
            if (_keyImage == null)
                return;

            bool pressed = G.Bindings.ToggleCompass.IsPressed;
            if (pressed == _pressed)
                return;

            _pressed = pressed;
            var sprite = pressed && _downSprite != null ? _downSprite : _upSprite;
            if (sprite != null)
                _keyImage.sprite = sprite;
        }

        private void RefreshIcon()
        {
            if (_keyImage == null)
                return;

            _upSprite = GetActionIcon(G.Bindings.ToggleCompass, false);
            _downSprite = GetActionIcon(G.Bindings.ToggleCompass, true);
            _keyImage.sprite = _pressed && _downSprite != null ? _downSprite : _upSprite;
            _keyImage.color = Color.white;
        }
    }
}
