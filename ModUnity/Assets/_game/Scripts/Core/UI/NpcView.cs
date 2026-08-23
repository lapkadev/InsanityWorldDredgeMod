using UnityEngine;
using UnityEngine.UI;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Params;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const float NPC_SLIDE_DURATION    = 0.34f;
        public const float NPC_FADE_DURATION     = 0.22f;
        public const float NPC_BACK_SLIDE_X      = -90f;
        public const float NPC_CHARACTER_SLIDE_X = -160f;
        public const float NPC_CHARACTER_DELAY   = 0.12f;
    }

    public static partial class Params
    {
        public static float P_NPC_SPEED_SCALE = 1f;
        public static float P_NPC_SLIDE_SCALE = 1f;
    }

    public class NpcView : MonoBehaviour
    {
        public RectTransform Background;
        public RectTransform Character;

        private Graphic _backGraphic;
        private Graphic _characterGraphic;
        private Vector2 _backHome;
        private Vector2 _characterHome;
        private float _time;
        private bool _ready;
        private bool _done;

        private void Awake()
        {
            if (Background != null)
            {
                _backGraphic = Background.GetComponent<Graphic>();
                _backHome = Background.anchoredPosition;
            }

            if (Character != null)
            {
                _characterGraphic = Character.GetComponent<Graphic>();
                _characterHome = Character.anchoredPosition;
            }

            _ready = Background != null || Character != null;
            if (!_ready)
                Log.Warn("NpcView: neither Background nor Character is assigned");
        }

        private void OnEnable()
        {
            _time = 0f;
            _done = false;
            Apply();
        }

        private void Update()
        {
            if (!_ready || _done)
                return;

            _time += Time.unscaledDeltaTime * Mathf.Max(0.01f, P_NPC_SPEED_SCALE);
            float total = NPC_CHARACTER_DELAY + Mathf.Max(NPC_SLIDE_DURATION, NPC_FADE_DURATION);
            if (_time >= total)
            {
                _time = total;
                _done = true;
            }

            Apply();
        }

        private void Apply()
        {
            if (!_ready)
                return;

            ApplyLayer(Background, _backGraphic, _backHome, NPC_BACK_SLIDE_X, 0f);
            ApplyLayer(Character, _characterGraphic, _characterHome, NPC_CHARACTER_SLIDE_X, NPC_CHARACTER_DELAY);
        }

        private static bool HasVisual(Graphic graphic)
        {
            if (graphic == null)
                return false;

            var image = graphic as Image;
            return image == null || image.sprite != null;
        }

        private void ApplyLayer(RectTransform layer, Graphic graphic, Vector2 home, float slideX, float delay)
        {
            if (layer == null || !HasVisual(graphic))
                return;

            float local = Mathf.Max(0f, _time - delay);
            float slide = Mathf.Clamp01(local / Mathf.Max(0.0001f, NPC_SLIDE_DURATION));
            float fade = Mathf.Clamp01(local / Mathf.Max(0.0001f, NPC_FADE_DURATION));
            float eased = 1f - (1f - slide) * (1f - slide);

            layer.anchoredPosition = home + new Vector2(slideX * P_NPC_SLIDE_SCALE * (1f - eased), 0f);
            if (graphic == null)
                return;

            var color = graphic.color;
            color.a = fade;
            graphic.color = color;
        }
    }
}
