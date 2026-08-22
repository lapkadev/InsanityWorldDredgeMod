using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.Core.Params;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        // Layout - fixed at UI creation time; not tunable at runtime (UI is built once in Start()).
        public const float MINIMAP_SIZE_PX                  = 280f;
        public const float MINIMAP_MARGIN_PX                = 20f;                      // gap from screen edges - wide enough for cardinal labels (15px half) + ~5px breathing room
        public const float MINIMAP_LABEL_FONT_SIZE_FALLBACK = 22f;
        public const float MINIMAP_TABS_BELOW_GAP_PX        = 10f;                      // small gap between minimap bottom and shifted tabs
        public const int   MINIMAP_CIRCLE_SPRITE_SIZE_PX    = 256;                      // generated mask texture resolution
        public const float MINIMAP_SHIP_ARROW_SIZE_PX       = 16f;                      // player triangle marker in minimap center
    }

    public static partial class Params
    {
        // Dynamic zoom - read every Update(); mutable so they can be live-tuned via Unity
        // Explorer: find the Params type (Static Fields view) and edit values on the fly.
        // Formula:
        //   target    = P_MINIMAP_ZOOM_AT_REST - speed * P_MINIMAP_ZOOM_PER_SPEED_UNIT  (clamped to MIN_FLOOR)
        //   displayed = Lerp(displayed, target, dt * P_MINIMAP_ZOOM_SMOOTH_RATE)
        public static float P_MINIMAP_ZOOM_AT_REST        = 1.40f;     // map scale when ship is stationary (zoomed in for detail)
        public static float P_MINIMAP_ZOOM_PER_SPEED_UNIT = 0.05f;     // PRIMARY TUNABLE: how much zoom shrinks per 1 world-unit/sec of speed
        public static float P_MINIMAP_ZOOM_MIN_FLOOR      = 0.001f;    // hard sanity guard only - Unity scale must stay > 0; not an aesthetic limit
        public static float P_MINIMAP_ZOOM_SMOOTH_RATE    = 2f;        // Lerp rate for visual easing (higher = snappier)
    }

    /// <summary>
    /// Minimap
    /// </summary>
    public class MinimapWidget : MonoBehaviour
    {
        private RectTransform _rotatingDial;
        private RectTransform _mapClone;
        private RectTransform _shipArrow;
        private float _worldToMapProportion;

        private RectTransform _embedParent;
        private float _diameter = MINIMAP_SIZE_PX;

        // Dynamic-zoom state. Seeded from the static initial value; updated each frame.
        private float _currentZoom = P_MINIMAP_ZOOM_AT_REST;
        private Vector3 _lastPlayerPos;                     // previous frame's player position, for speed calc
        private bool _hasLastPlayerPos;                     // false until first valid sample captured

        private static TMPro.TMP_FontAsset _dredgeFont;
        private static float _dredgeFontSize;
        private static bool _dredgeStyleResolved;

        public void EmbedInto(RectTransform parent)
        {
            _embedParent = parent;
            _diameter = Mathf.Min(parent.rect.width, parent.rect.height);
        }

        public void Start()
        {
            Transform parent = _embedParent;
            if (parent == null)
            {
                if (G.GameCanvas == null)
                {
                    Log.Warn("MinimapWidget: game canvas not available");
                    return;
                }

                parent = G.GameCanvas;
            }

            TryResolveDredgeCompassStyle();

            var root = new GameObject("MinimapRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(_diameter, _diameter);

            if (_embedParent == null)
            {
                // Render BEHIND all Dredge HUD elements in the same canvas (cargo panel, inventory grid, etc.).
                // SetSiblingIndex(0) = first child = drawn first = covered by later siblings when they overlap.
                root.transform.SetAsFirstSibling();
                AnchorToCorner(rootRt, MINIMAP_CORNER, MINIMAP_MARGIN_PX);
            }
            else
            {
                rootRt.anchorMin = rootRt.anchorMax = rootRt.pivot = new Vector2(0.5f, 0.5f);
                rootRt.anchoredPosition = Vector2.zero;
            }

            // Background circle
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(Mask));
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(_diameter, _diameter);
            var bgImage = bgGo.GetComponent<Image>();

            // Use a runtime-generated circular sprite for map mask (alpha=0 outside circle).
            bgImage.sprite = GetCircleSprite();
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);
            bgGo.GetComponent<Mask>().showMaskGraphic = true;

            // Clone Dredge MapContents under Background so it's clipped by the Mask.
            // Position/rotation/scale are driven each Update() to keep player at center
            // and minimap heading-up relative to camera yaw.
            TryCloneDredgeMap(bgGo.transform);

            // Rotating dial - holds the four cardinal labels. Rotating this transform
            // moves all labels together; the background stays static.
            var dialGo = new GameObject("Dial", typeof(RectTransform));
            dialGo.transform.SetParent(root.transform, false);
            _rotatingDial = dialGo.GetComponent<RectTransform>();
            _rotatingDial.anchorMin = _rotatingDial.anchorMax = new Vector2(0.5f, 0.5f);
            _rotatingDial.pivot = new Vector2(0.5f, 0.5f);
            _rotatingDial.sizeDelta = new Vector2(_diameter, _diameter);

            // 4 cardinals at (0, +R) (+R, 0) (0, -R) (-R, 0) - N E S W.
            float labelRadius = _diameter * 0.5f;
            AddCardinal("N", new Vector2(0f,  labelRadius), Color.red);
            AddCardinal("E", new Vector2( labelRadius, 0f), Color.white);
            AddCardinal("S", new Vector2(0f, -labelRadius), Color.white);
            AddCardinal("W", new Vector2(-labelRadius, 0f), Color.white);

            // Ship direction arrow at the very center of the minimap. 
            var arrowGo = new GameObject("ShipArrow", typeof(RectTransform), typeof(Image));
            arrowGo.transform.SetParent(root.transform, false);
            _shipArrow = arrowGo.GetComponent<RectTransform>();
            _shipArrow.anchorMin = _shipArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _shipArrow.pivot = new Vector2(0.5f, 0.5f);
            float arrowSize = MINIMAP_SHIP_ARROW_SIZE_PX * Scale;
            _shipArrow.sizeDelta = new Vector2(arrowSize, arrowSize);
            _shipArrow.anchoredPosition = Vector2.zero;
            var arrowImg = arrowGo.GetComponent<Image>();
            arrowImg.sprite = GetArrowSprite();
            arrowImg.color = Color.white;

            if (_embedParent == null)
            {
                Log.Debug($"MinimapWidget: created in {MINIMAP_CORNER} corner");
                ShiftSlidePanelTabBelowMinimap();
            }
            else
            {
                Log.Debug($"MinimapWidget: created embedded in '{_embedParent.name}', diameter {_diameter}");
            }
        }

        private float Scale => _diameter / MINIMAP_SIZE_PX;

        private void ShiftSlidePanelTabBelowMinimap()
        {
#pragma warning disable CS0162
            if (MINIMAP_CORNER != HudCorner.TopRight)
                return;
#pragma warning restore CS0162

            float minimapBottomY = Screen.height - MINIMAP_MARGIN_PX - MINIMAP_SIZE_PX;
            ShiftHudTabBelow(minimapBottomY - MINIMAP_TABS_BELOW_GAP_PX);
        }

        private void TryCloneDredgeMap(Transform parent)
        {
            _mapClone = CreateMapClone();
            if (_mapClone == null)
            {
                Log.Warn("MinimapWidget: map clone not available");
                return;
            }

            _worldToMapProportion = GetMapPixelsPerWorldUnit();

            _mapClone.SetParent(parent, false);
            _mapClone.anchorMin = _mapClone.anchorMax = new Vector2(0.5f, 0.5f);
            _mapClone.pivot = new Vector2(0.5f, 0.5f);
            _mapClone.anchoredPosition = Vector2.zero;

            Log.Info($"MinimapWidget: map clone attached (proportion={_worldToMapProportion})");
        }

        public void Update()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var camYaw = cam.transform.eulerAngles.y;

            if (_rotatingDial != null)
            {
                _rotatingDial.localEulerAngles = new Vector3(0f, 0f, camYaw);
                foreach (Transform label in _rotatingDial)
                    label.localEulerAngles = new Vector3(0f, 0f, -camYaw);
            }

            UpdateMapClone(camYaw);
            UpdateShipArrow(camYaw);
        }

        /// <summary>
        /// Rotate the centre arrow to point in the player ship's actual world heading.
        /// </summary>
        private void UpdateShipArrow(float camYaw)
        {
            if (_shipArrow == null)
                return;

            var player = GetPlayerTransform();
            if (player == null)
                return;

            float shipYaw = player.eulerAngles.y;
            _shipArrow.localEulerAngles = new Vector3(0f, 0f, camYaw - shipYaw);
        }

        /// <summary>
        /// Update the cloned map's transform each frame
        /// </summary>
        private void UpdateMapClone(float camYaw)
        {
            if (_mapClone == null)
                return;

            var player = GetPlayerTransform();
            if (player == null)
                return;

            var pos = player.position;

            // Dynamic zoom
            if (_hasLastPlayerPos && Time.deltaTime > 0f)
            {
                float speed = (pos - _lastPlayerPos).magnitude / Time.deltaTime;
                float targetZoom = Mathf.Max(
                    P_MINIMAP_ZOOM_AT_REST - speed * P_MINIMAP_ZOOM_PER_SPEED_UNIT,
                    P_MINIMAP_ZOOM_MIN_FLOOR);
                _currentZoom = Mathf.Lerp(_currentZoom, targetZoom, Time.deltaTime * P_MINIMAP_ZOOM_SMOOTH_RATE);
            }
            _lastPlayerPos = pos;
            _hasLastPlayerPos = true;

            var mapPos = new Vector2(pos.x * _worldToMapProportion, pos.z * _worldToMapProportion) / 0.95f;


            float angleDeg = camYaw;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
            var rotatedMapPos = new Vector2(
                mapPos.x * cos - mapPos.y * sin,
                mapPos.x * sin + mapPos.y * cos);

            _mapClone.localScale       = Vector3.one * _currentZoom;
            _mapClone.anchoredPosition = -rotatedMapPos * _currentZoom;
            _mapClone.localEulerAngles = new Vector3(0f, 0f, angleDeg);
        }

        private void AddCardinal(string letter, Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(letter, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_rotatingDial, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(30f, 30f) * Scale;
            rt.anchoredPosition = anchoredPos;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = letter;
            if (_dredgeFont != null)
                tmp.font = _dredgeFont;

            tmp.fontSize = (_dredgeFontSize > 0f ? _dredgeFontSize : MINIMAP_LABEL_FONT_SIZE_FALLBACK) * Scale;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
        }

        private static void TryResolveDredgeCompassStyle()
        {
            if (_dredgeStyleResolved)
                return;

            _dredgeStyleResolved = true;

            _dredgeFont = GetDredgeCompassFont();
            _dredgeFontSize = GetDredgeCompassFontSize();

            Log.Debug($"MinimapWidget: matched Dredge font '{(_dredgeFont != null ? _dredgeFont.name : "?")}', size {_dredgeFontSize}");
        }

        private static Sprite _circleSpriteCache;
        private static Sprite GetCircleSprite()
        {
            if (_circleSpriteCache != null)
                return _circleSpriteCache;

            int n = MINIMAP_CIRCLE_SPRITE_SIZE_PX;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, mipChain: false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[n * n];
            float center = n * 0.5f;
            float radius = center - 1f;  // leave 1px transparent border for AA

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);  // 1px soft edge
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false);
            _circleSpriteCache = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
            return _circleSpriteCache;
        }

        private static Sprite _arrowSpriteCache;
        private static Sprite GetArrowSprite()
        {
            if (_arrowSpriteCache != null)
                return _arrowSpriteCache;

            int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, mipChain: false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[n * n];
            var darkGreen  = new Color(0.05f, 0.30f, 0.08f);   // edges (deep forest)
            var lightLime  = new Color(0.65f, 0.95f, 0.25f);   // interior
            float gradientReach = n * 0.25f;                    // distance over which we ramp from dark to light

            for (int y = 0; y < n; y++)
            {
                float halfWidth = (n - y) * 0.5f;
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - n * 0.5f);
                    float sideDist   = halfWidth - dx;   // distance to nearest side edge
                    float bottomDist = y;                // distance to bottom edge
                    float edgeDist   = Mathf.Min(sideDist, bottomDist);

                    float alpha = Mathf.Clamp01(edgeDist);                    // 1px soft border
                    float t     = Mathf.Clamp01(edgeDist / gradientReach);    // 0 at edge, 1 deep inside
                    var col = Color.Lerp(darkGreen, lightLime, t);
                    pixels[y * n + x] = new Color32(
                        (byte)(col.r * 255f),
                        (byte)(col.g * 255f),
                        (byte)(col.b * 255f),
                        (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false);
            _arrowSpriteCache = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
            return _arrowSpriteCache;
        }

    }
}
