using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string COMPASS_PREFAB_NAME            = "pfb_insanity_compas_map";
        public const string COMPASS_ROOT_NAME              = "InsanityCompassRoot";
        public const string COMPASS_MINIMAP_HOST_NAME      = "CompassMinimap";
        public const float  COMPASS_MARGIN_PX              = 0f;
        public const bool   COMPASS_VISIBLE_ON_START       = false;
        public const float  COMPASS_OFFSCREEN_PADDING_PX   = 40f;
        public const float  COMPASS_SLIDE_SPEED_PX_PER_SEC = 2200f;
    }

    public static partial class Funcs
    {
        public static void SetChildrenActive(Transform root, bool active)
        {
            foreach (Transform child in root)
                child.gameObject.SetActive(active);
        }
    }

    public class CompassWidget : MonoBehaviour
    {
        public RectTransform MapAnchor;

        private RectTransform _root;
        private GameObject _prompt;
        private bool _wantedByPlayer = COMPASS_VISIBLE_ON_START;
        private Vector2 _shownPos;
        private Vector2 _hiddenPos;
        private bool _placed;
        private bool _visualsActive = true;

        public static GameObject TryCreate()
        {
            if (G.GameCanvas == null)
            {
                Log.Warn("CompassWidget: game canvas not available");
                return null;
            }

            if (!G.Prefabs.TryGetValue(COMPASS_PREFAB_NAME, out var prefab) || prefab == null)
            {
                Log.Warn($"CompassWidget: prefab '{COMPASS_PREFAB_NAME}' not found among loaded bundles");
                return null;
            }

            var obj = CloneUiNode(prefab, COMPASS_ROOT_NAME, G.GameCanvas);
            obj.transform.SetAsFirstSibling();
            return obj;
        }

        public void Start()
        {
            var rootRt = GetComponent<RectTransform>();
            if (rootRt == null)
            {
                Log.Warn("CompassWidget: prefab root has no RectTransform");
                return;
            }

            if (MapAnchor == null)
            {
                Log.Warn("CompassWidget: MapAnchor is not assigned - compass will show no map");
                return;
            }

            rootRt.sizeDelta = FrameSize(rootRt);
            AnchorToCorner(rootRt, HudCorner.BottomRight, COMPASS_MARGIN_PX);

            var mapHost = new GameObject(COMPASS_MINIMAP_HOST_NAME);
            mapHost.transform.SetParent(MapAnchor, false);
            mapHost.AddComponent<MinimapWidget>().EmbedInto(MapAnchor);

            _root = rootRt;
            _shownPos = rootRt.anchoredPosition;
            _hiddenPos = _shownPos + HideOffset(rootRt.rect.size);
            _prompt = CompassHotkeyPrompt.TryCreate();

            Log.Info($"CompassWidget: created, dial {MapAnchor.rect.width}x{MapAnchor.rect.height}");
        }

        public void Update()
        {
            if (_root == null)
                return;

            bool sailing = IsPlayerSailing();
            if (sailing && G.Bindings.ToggleCompass.WasPressed)
                _wantedByPlayer = !_wantedByPlayer;

            if (_prompt != null && _prompt.activeSelf != sailing)
                _prompt.SetActive(sailing);

            bool visible = _wantedByPlayer && sailing;
            var target = visible ? _shownPos : _hiddenPos;

            if (!_placed)
            {
                _placed = true;
                _root.anchoredPosition = target;
                SetVisualsActive(visible);
                return;
            }

            if (visible)
                SetVisualsActive(true);

            _root.anchoredPosition = Vector2.MoveTowards(
                _root.anchoredPosition, target, COMPASS_SLIDE_SPEED_PX_PER_SEC * Time.unscaledDeltaTime);

            if (!visible && _root.anchoredPosition == target)
                SetVisualsActive(false);
        }

        private void SetVisualsActive(bool active)
        {
            if (_visualsActive == active)
                return;

            _visualsActive = active;
            SetChildrenActive(_root, active);
        }

        private static Vector2 HideOffset(Vector2 size) =>
            new Vector2(0f, -(size.y + COMPASS_OFFSCREEN_PADDING_PX));

        private static Vector2 FrameSize(RectTransform root)
        {
            var size = root.sizeDelta;
            foreach (Transform child in root)
            {
                var rt = child as RectTransform;
                if (rt == null)
                    continue;

                if (rt.rect.width * rt.rect.height > size.x * size.y)
                    size = new Vector2(rt.rect.width, rt.rect.height);
            }
            return size;
        }
    }
}
