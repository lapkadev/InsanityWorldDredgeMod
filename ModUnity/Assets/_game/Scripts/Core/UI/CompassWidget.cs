using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string  COMPASS_PREFAB_NAME     = "InsanityCompasMap";
        public const string  COMPASS_MAP_ANCHOR_NAME = "MapAnchor";
        public const float   COMPASS_MARGIN_PX       = 0f;
        public const bool    COMPASS_VISIBLE_ON_START = false;
        public const float   COMPASS_OFFSCREEN_PADDING_PX = 40f;
        public const float   COMPASS_SLIDE_SPEED_PX_PER_SEC = 2200f;
    }

    public class CompassWidget : MonoBehaviour
    {
        private RectTransform _root;
        private GameObject _prompt;
        private bool _wantedByPlayer = COMPASS_VISIBLE_ON_START;
        private Vector2 _shownPos;
        private Vector2 _hiddenPos;
        private bool _placed;

        public void Start()
        {
            var canvas = GameObject.Find("GameCanvases/GameCanvas");
            if (canvas == null) { G.Log.Warn("CompassWidget: GameCanvas not found"); return; }

            if (!G.Prefabs.TryGetValue(COMPASS_PREFAB_NAME, out var prefab) || prefab == null)
            {
                G.Log.Warn($"CompassWidget: prefab '{COMPASS_PREFAB_NAME}' not found among loaded bundles");
                return;
            }

            var obj = Object.Instantiate(prefab, canvas.transform, false);
            obj.name = "InsanityCompassRoot";
            obj.transform.SetAsFirstSibling();

            var rootRt = obj.GetComponent<RectTransform>();
            if (rootRt == null) { G.Log.Warn("CompassWidget: prefab root has no RectTransform"); return; }

            rootRt.sizeDelta = FrameSize(rootRt);
            AnchorToCorner(rootRt, HudCorner.BottomRight, COMPASS_MARGIN_PX);

            var anchor = rootRt.Find(COMPASS_MAP_ANCHOR_NAME) as RectTransform;
            if (anchor == null)
            {
                G.Log.Warn($"CompassWidget: '{COMPASS_MAP_ANCHOR_NAME}' not found in prefab - compass will show no map");
                return;
            }

            var mapHost = new GameObject("CompassMinimap");
            mapHost.transform.SetParent(anchor, false);
            mapHost.AddComponent<MinimapWidget>().EmbedInto(anchor);

            _root = rootRt;
            _shownPos = rootRt.anchoredPosition;
            _hiddenPos = _shownPos + HideOffset(rootRt.rect.size);
            _prompt = CompassHotkeyPrompt.TryCreate();

            G.Log.Info($"CompassWidget: created, dial {anchor.rect.width}x{anchor.rect.height}");
        }

        public void Update()
        {
            if (_root == null) return;

            bool sailing = IsPlayerSailing();
            if (sailing && G.Bindings != null && G.Bindings.ToggleCompass.WasPressed)
                _wantedByPlayer = !_wantedByPlayer;

            if (_prompt != null && _prompt.activeSelf != sailing)
                _prompt.SetActive(sailing);

            bool visible = _wantedByPlayer && sailing;
            var target = visible ? _shownPos : _hiddenPos;

            if (!_placed)
            {
                _placed = true;
                _root.anchoredPosition = target;
                _root.gameObject.SetActive(visible);
                return;
            }

            if (visible && !_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);

            _root.anchoredPosition = Vector2.MoveTowards(
                _root.anchoredPosition, target, COMPASS_SLIDE_SPEED_PX_PER_SEC * Time.unscaledDeltaTime);

            if (!visible && _root.anchoredPosition == target && _root.gameObject.activeSelf)
                _root.gameObject.SetActive(false);
        }

        private static Vector2 HideOffset(Vector2 size) =>
            new Vector2(0f, -(size.y + COMPASS_OFFSCREEN_PADDING_PX));

        private static Vector2 FrameSize(RectTransform root)
        {
            var size = root.sizeDelta;
            foreach (Transform child in root)
            {
                var rt = child as RectTransform;
                if (rt == null) continue;
                if (rt.rect.width * rt.rect.height > size.x * size.y)
                    size = new Vector2(rt.rect.width, rt.rect.height);
            }
            return size;
        }
    }
}
