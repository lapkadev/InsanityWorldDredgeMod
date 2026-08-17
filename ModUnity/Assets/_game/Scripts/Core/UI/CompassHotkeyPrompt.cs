using System;
using InControl;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string COMPASS_ICON_PREFAB_NAME = "pfb_ui_key_compas_map";
        public const string COMPASS_PROMPT_NAME      = "InsanityCompassPrompt";
        public const string COMPASS_PROMPT_KEY_NODE  = "Key";
        public const string GAME_CANVAS_PATH         = "GameCanvases/GameCanvas";
        public const float  COMPASS_PROMPT_POS_X     = 40f;
        public const float  COMPASS_PROMPT_POS_Y     = 260f;
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
                G.Log.Warn($"CompassHotkeyPrompt: prefab '{COMPASS_ICON_PREFAB_NAME}' not found among loaded bundles");
                return null;
            }

            var canvas = GameObject.Find(GAME_CANVAS_PATH);
            if (canvas == null) { G.Log.Warn("CompassHotkeyPrompt: GameCanvas not found"); return null; }

            var obj = UnityEngine.Object.Instantiate(prefab, canvas.transform, false);
            obj.name = COMPASS_PROMPT_NAME;

            var rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(COMPASS_PROMPT_POS_X, COMPASS_PROMPT_POS_Y);
            }

            obj.AddComponent<CompassHotkeyPrompt>();

            G.Log.Info("CompassHotkeyPrompt: created");
            return obj;
        }

        public void Start()
        {
            var node = transform.Find(COMPASS_PROMPT_KEY_NODE);
            if (node == null) { G.Log.Warn($"CompassHotkeyPrompt: node '{COMPASS_PROMPT_KEY_NODE}' not found in prefab"); return; }

            _keyImage = node.GetComponent<Image>();
            if (_keyImage == null) { G.Log.Warn($"CompassHotkeyPrompt: node '{COMPASS_PROMPT_KEY_NODE}' has no Image"); return; }

            RefreshIcon();

            var input = GameManager.Instance?.Input;
            if (input == null) return;

            _onInputChanged = (source, style) => RefreshIcon();
            input.OnInputChanged = (Action<BindingSourceType, InputDeviceStyle>)Delegate.Combine(input.OnInputChanged, _onInputChanged);
        }

        public void OnDestroy()
        {
            var input = GameManager.Instance?.Input;
            if (input == null || _onInputChanged == null) return;

            input.OnInputChanged = (Action<BindingSourceType, InputDeviceStyle>)Delegate.Remove(input.OnInputChanged, _onInputChanged);
            _onInputChanged = null;
        }

        public void Update()
        {
            if (_keyImage == null || G.Bindings == null) return;

            bool pressed = G.Bindings.ToggleCompass.IsPressed;
            if (pressed == _pressed) return;

            _pressed = pressed;
            var sprite = pressed && _downSprite != null ? _downSprite : _upSprite;
            if (sprite != null) _keyImage.sprite = sprite;
        }

        private void RefreshIcon()
        {
            if (_keyImage == null || G.Bindings == null) return;

            var icon = GameManager.Instance?.Input?.GetControlIconForActionWithDefault(G.Bindings.ToggleCompass);
            if (icon == null) { G.Log.Warn("CompassHotkeyPrompt: no control icon for ToggleCompass"); return; }

            _upSprite = icon.upSprite;
            _downSprite = icon.downSprite;
            _keyImage.sprite = _pressed && _downSprite != null ? _downSprite : _upSprite;
            _keyImage.color = Color.white;
        }
    }
}
