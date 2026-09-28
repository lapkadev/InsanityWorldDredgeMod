using System.Collections.Generic;
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
        public const bool MINIMAP_CORNER_WIDGET_ENABLED     = false;

        // Layout - fixed at UI creation time; not tunable at runtime (UI is built once in Start()).
        public const float MINIMAP_SIZE_PX                  = 280f;
        public const float MINIMAP_MARGIN_PX                = 20f;                      // gap from screen edges - wide enough for cardinal labels (15px half) + ~5px breathing room
        public const float MINIMAP_LABEL_FONT_SIZE_FALLBACK = 22f;
        public const float MINIMAP_TABS_BELOW_GAP_PX        = 10f;                      // small gap between minimap bottom and shifted tabs
        public const int   MINIMAP_CIRCLE_SPRITE_SIZE_PX    = 256;                      // generated mask texture resolution
        public const int   MINIMAP_ARROW_SPRITE_SIZE_PX     = 64;                       // generated ship-arrow texture resolution
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
        private Image _islandPointer;
        private Image _islandMark;
        private RawImage _fieldLayer;
        private Texture2D _fieldTexture;
        private Color32[] _fieldPixels;
        private Vector3 _fieldCenter;
        private float _fieldSideM;
        private float _nextFieldRefreshTime;
        private RectTransform[] _markLayers;
        private CanvasGroup[] _markGroups;
        private List<Image>[] _markBlips;
        private RectTransform[] _outlineLayers;
        private List<Image>[] _outlineBlips;
        private int[] _shownPerLayer;
        private float _nextThreatScanTime;
        private float _worldToMapProportion;

        private RectTransform _embedParent;
        private float _diameter = MINIMAP_SIZE_PX;

        // Dynamic-zoom state. Seeded from the static initial value; updated each frame.
        private float _currentZoom = P_MINIMAP_ZOOM_AT_REST;
        private float _currentSpeed;
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
            var objBg = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(Mask));
            objBg.transform.SetParent(root.transform, false);
            var bgRt = objBg.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(_diameter, _diameter);
            var bgImage = objBg.GetComponent<Image>();

            // Use a runtime-generated circular sprite for map mask (alpha=0 outside circle).
            bgImage.sprite = GetCircleSprite();
            bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);
            objBg.GetComponent<Mask>().showMaskGraphic = true;

            // Clone Dredge MapContents under Background so it's clipped by the Mask.
            // Position/rotation/scale are driven each Update() to keep player at center
            // and minimap heading-up relative to camera yaw.
            TryCloneDredgeMap(objBg.transform);

            _fieldLayer = CreateFieldLayer(objBg.transform);
            CreateMarkLayers(objBg.transform);
            _islandMark = CreateIslandImage(objBg.transform, ISLAND_MARK_NAME, GetCircleSprite(), ISLAND_MARK_SIZE_PX);

            // Rotating dial - holds the four cardinal labels. Rotating this transform
            // moves all labels together; the background stays static.
            var objDial = new GameObject("Dial", typeof(RectTransform));
            objDial.transform.SetParent(root.transform, false);
            _rotatingDial = objDial.GetComponent<RectTransform>();
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
            var objArrow = new GameObject("ShipArrow", typeof(RectTransform), typeof(Image));
            objArrow.transform.SetParent(root.transform, false);
            _shipArrow = objArrow.GetComponent<RectTransform>();
            _shipArrow.anchorMin = _shipArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _shipArrow.pivot = new Vector2(0.5f, 0.5f);
            float arrowSize = MINIMAP_SHIP_ARROW_SIZE_PX * Scale;
            _shipArrow.sizeDelta = new Vector2(arrowSize, arrowSize);
            _shipArrow.anchoredPosition = Vector2.zero;
            var arrowImg = objArrow.GetComponent<Image>();
            arrowImg.sprite = GetArrowSprite();
            arrowImg.color = Color.white;

            _islandPointer = CreateIslandImage(root.transform, ISLAND_POINTER_NAME, GetIslandPointerSprite(), ISLAND_POINTER_SIZE_PX);

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
            UpdateFieldLayer(camYaw);
            UpdateShipArrow(camYaw);
            UpdateThreatBlips(camYaw);
            UpdateIslandPointer(camYaw);
        }

        private RawImage CreateFieldLayer(Transform parent)
        {
            var obj = new GameObject(FIELD_LAYER_NAME, typeof(RectTransform), typeof(RawImage));
            obj.transform.SetParent(parent, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            int n = FIELD_LAYER_TEXTURE_PX;
            _fieldTexture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            _fieldTexture.wrapMode = TextureWrapMode.Clamp;
            _fieldTexture.filterMode = FilterMode.Bilinear;
            _fieldPixels = new Color32[n * n];

            var img = obj.GetComponent<RawImage>();
            img.texture = _fieldTexture;
            img.raycastTarget = false;

            obj.SetActive(false);
            return img;
        }

        private void UpdateFieldLayer(float camYaw)
        {
            if (_fieldLayer == null || _worldToMapProportion <= 0f)
                return;

            var player = GetPlayerTransform();
            if (player == null || !IsInsanityFieldActive())
            {
                _fieldLayer.gameObject.SetActive(false);
                return;
            }

            float pixelsPerWorldUnit = _worldToMapProportion / 0.95f * _currentZoom;
            if (pixelsPerWorldUnit <= 0f)
                return;

            var pos = player.position;
            float neededSideM = _diameter / pixelsPerWorldUnit * FIELD_LAYER_MARGIN;
            float recenterM = _fieldSideM * FIELD_LAYER_RECENTER_SHARE;
            bool stale = Time.unscaledTime >= _nextFieldRefreshTime
                || neededSideM > _fieldSideM
                || Mathf.Abs(pos.x - _fieldCenter.x) > recenterM
                || Mathf.Abs(pos.z - _fieldCenter.z) > recenterM;

            if (stale)
            {
                _nextFieldRefreshTime = Time.unscaledTime + FIELD_LAYER_REFRESH_SEC;
                _fieldCenter = pos;
                _fieldSideM = neededSideM;
                FillInsanityFieldTexture(_fieldPixels, FIELD_LAYER_TEXTURE_PX, _fieldCenter, _fieldSideM);
                _fieldTexture.SetPixels32(_fieldPixels);
                _fieldTexture.Apply(false);
            }

            float angleRad = camYaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
            float dx = (_fieldCenter.x - pos.x) * pixelsPerWorldUnit;
            float dz = (_fieldCenter.z - pos.z) * pixelsPerWorldUnit;

            var rt = _fieldLayer.rectTransform;
            rt.sizeDelta = Vector2.one * _fieldSideM * pixelsPerWorldUnit;
            rt.anchoredPosition = new Vector2(dx * cos - dz * sin, dx * sin + dz * cos);
            rt.localEulerAngles = new Vector3(0f, 0f, camYaw);

            _fieldLayer.color = new Color(1f, 1f, 1f, GetInsanityFieldLayerClarity(_currentSpeed));
            _fieldLayer.gameObject.SetActive(true);
        }

        private Image CreateIslandImage(Transform parent, string objName, Sprite sprite, float sizePx)
        {
            var obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sizePx, sizePx) * Scale;

            var img = obj.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;

            obj.SetActive(false);
            return img;
        }

        private void UpdateIslandPointer(float camYaw)
        {
            if (_islandPointer == null || _islandMark == null)
                return;

            var player = GetPlayerTransform();
            if (G.PortalIsland == null || player == null)
            {
                _islandPointer.gameObject.SetActive(false);
                _islandMark.gameObject.SetActive(false);
                return;
            }

            var islandPos = G.PortalIsland.transform.position;
            var playerPos = player.position;
            float worldDx = islandPos.x - playerPos.x;
            float worldDz = islandPos.z - playerPos.z;

            float angleRad = camYaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
            var local = new Vector2(worldDx * cos - worldDz * sin, worldDx * sin + worldDz * cos);

            float radius = _diameter * 0.5f;
            float pixelsPerWorldUnit = _worldToMapProportion / 0.95f * _currentZoom;
            bool inside = pixelsPerWorldUnit > 0f && local.magnitude * pixelsPerWorldUnit <= radius;

            var color = ISLAND_POINTER_COLOR;
            color.a = GetIslandPointerClarity(_currentSpeed);

            _islandMark.gameObject.SetActive(inside);
            _islandPointer.gameObject.SetActive(!inside);

            if (inside)
            {
                _islandMark.rectTransform.anchoredPosition = local * pixelsPerWorldUnit;
                _islandMark.color = color;
                return;
            }

            var direction = local.normalized;
            float pointerYaw = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _islandPointer.rectTransform.anchoredPosition = direction * (radius - ISLAND_POINTER_INSET_PX * Scale);
            _islandPointer.rectTransform.localEulerAngles = new Vector3(0f, 0f, pointerYaw);
            _islandPointer.color = color;
        }

        private void CreateMarkLayers(Transform parent)
        {
            int count = MARK_LAYER_NAMES.Length;
            _markLayers = new RectTransform[count];
            _markGroups = new CanvasGroup[count];
            _markBlips = new List<Image>[count];
            _outlineLayers = new RectTransform[count];
            _outlineBlips = new List<Image>[count];
            _shownPerLayer = new int[count];

            for (int i = 0; i < count; i++)
            {
                _outlineLayers[i] = CreateBlipLayer(parent, MARK_LAYER_NAMES[i] + MARK_OUTLINE_LAYER_SUFFIX, out _);
                _outlineBlips[i] = new List<Image>();
                _markLayers[i] = CreateBlipLayer(parent, MARK_LAYER_NAMES[i], out _markGroups[i]);
                _markBlips[i] = new List<Image>();
            }
        }

        private RectTransform CreateBlipLayer(Transform parent, string layerName, out CanvasGroup group)
        {
            var objLayer = new GameObject(layerName, typeof(RectTransform), typeof(CanvasGroup));
            objLayer.transform.SetParent(parent, false);

            var layerRt = objLayer.GetComponent<RectTransform>();
            layerRt.anchorMin = layerRt.anchorMax = layerRt.pivot = new Vector2(0.5f, 0.5f);
            layerRt.sizeDelta = new Vector2(_diameter, _diameter);
            layerRt.anchoredPosition = Vector2.zero;

            group = objLayer.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            return layerRt;
        }

        private static Image GetPooledBlip(List<Image> pool, RectTransform layer, string blipName, int index)
        {
            while (pool.Count <= index)
            {
                var obj = new GameObject(blipName, typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(layer, false);

                var rt = obj.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

                var img = obj.GetComponent<Image>();
                img.sprite = GetCircleSprite();
                img.raycastTarget = false;

                obj.SetActive(false);
                pool.Add(img);
            }

            return pool[index];
        }

        private void UpdateThreatBlips(float camYaw)
        {
            if (_markLayers == null || _worldToMapProportion <= 0f)
                return;

            var player = GetPlayerTransform();
            if (player == null)
                return;

            if (Time.unscaledTime >= _nextThreatScanTime)
            {
                _nextThreatScanTime = Time.unscaledTime + THREAT_SCAN_INTERVAL_SEC;
                RefreshThreats();
            }

            for (int i = 0; i < _markGroups.Length; i++)
            {
                _markGroups[i].alpha = GetMarkLayerBlinkAlpha(i);
                _shownPerLayer[i] = 0;
            }

            float pixelsPerWorldUnit = _worldToMapProportion / 0.95f * _currentZoom;
            float radius = _diameter * 0.5f;

            float angleRad = camYaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
            var playerPos = player.position;
            float radiusSq = radius * radius;

            for (int i = 0; i < G.Threats.Count; i++)
            {
                var mark = G.Threats[i];
                var node = mark.Node;
                if (node == null)
                    continue;

                var markPos = node.position;
                float worldDx = markPos.x - playerPos.x;
                float worldDz = markPos.z - playerPos.z;
                float distanceM = Mathf.Sqrt(worldDx * worldDx + worldDz * worldDz);
                float alpha = GetCompassRangeAlpha(distanceM, mark.Kind) * GetCompassClarity(_currentSpeed, mark.Kind);
                if (alpha <= 0f)
                    continue;

                float dx = worldDx * pixelsPerWorldUnit;
                float dz = worldDz * pixelsPerWorldUnit;
                var local = new Vector2(dx * cos - dz * sin, dx * sin + dz * cos);
                if (local.sqrMagnitude > radiusSq)
                    continue;

                int layer = GetMinimapMarkLayer(mark.Kind);
                int slot = _shownPerLayer[layer]++;
                var blip = GetPooledBlip(_markBlips[layer], _markLayers[layer], MARK_BLIP_NAME, slot);

                float markSizePx = GetMinimapMarkSizePx(mark.Kind);
                float markSize = markSizePx * Scale;
                blip.rectTransform.sizeDelta = new Vector2(markSize, markSize);
                blip.rectTransform.anchoredPosition = local;
                var blipColor = GetMinimapMarkColor(mark.Kind);
                blipColor.a = alpha;
                blip.color = blipColor;
                blip.gameObject.SetActive(true);

                float outlineSizePx = markSizePx + MARK_OUTLINE_WIDTH_PX * 2f;
                float outlineSize = outlineSizePx * Scale;
                var outline = GetPooledBlip(_outlineBlips[layer], _outlineLayers[layer], MARK_OUTLINE_NAME, slot);
                outline.sprite = GetRingSprite(markSizePx / outlineSizePx);
                outline.rectTransform.sizeDelta = new Vector2(outlineSize, outlineSize);
                outline.rectTransform.anchoredPosition = local;
                var outlineColor = GetMinimapMarkOutlineColor(mark.Kind);
                outlineColor.a = alpha;
                outline.color = outlineColor;
                outline.gameObject.SetActive(true);
            }

            for (int i = 0; i < _markBlips.Length; i++)
            {
                HideBlipsFrom(_markBlips[i], _shownPerLayer[i]);
                HideBlipsFrom(_outlineBlips[i], _shownPerLayer[i]);
            }
        }

        private static void HideBlipsFrom(List<Image> pool, int start)
        {
            for (int i = start; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
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
                _currentSpeed = Mathf.Lerp(_currentSpeed, speed, Time.deltaTime * P_MINIMAP_ZOOM_SMOOTH_RATE);
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
            var obj = new GameObject(letter, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(_rotatingDial, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(30f, 30f) * Scale;
            rt.anchoredPosition = anchoredPos;

            var tmp = obj.GetComponent<TextMeshProUGUI>();
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

        private static readonly Dictionary<float, Sprite> _ringSpriteCache = new Dictionary<float, Sprite>();
        private static Sprite GetRingSprite(float innerFraction)
        {
            if (_ringSpriteCache.TryGetValue(innerFraction, out var cached))
                return cached;

            int n = MINIMAP_CIRCLE_SPRITE_SIZE_PX;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, mipChain: false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[n * n];
            float center = n * 0.5f;
            float outer = center - 1f;
            float inner = outer * innerFraction;

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(outer - dist) * Mathf.Clamp01(dist - inner);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
            _ringSpriteCache[innerFraction] = sprite;
            return sprite;
        }

        private static Sprite _arrowSpriteCache;
        private static Sprite GetArrowSprite()
        {
            if (_arrowSpriteCache != null)
                return _arrowSpriteCache;

            int n = MINIMAP_ARROW_SPRITE_SIZE_PX;
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
