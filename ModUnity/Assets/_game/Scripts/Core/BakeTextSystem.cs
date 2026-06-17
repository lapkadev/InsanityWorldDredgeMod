using TMPro;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const int BAKE_TEXT_WIDTH = 2048;
        public const int BAKE_TEXT_HEIGHT = 128;
        public const int BAKE_TEXT_LAYER = 31;
    }

    public static partial class Funcs
    {
        private static Camera _bakeCam;
        private static TextMeshPro _bakeTmp;
        private static Renderer _bakeRenderer;
        private static float _emWorld;
        private static float _worldWidth;

        public static Texture2D BakeText(string text)
        {
            if (G.InsanityFont == null)
            {
                G.Log.Warn("BakeText: InsanityFont is not loaded");
                return null;
            }

            EnsureBaker();
            
            string raw = text ?? string.Empty;
            int count = Mathf.Max(raw.Length, 1);
            float cellEm = (_worldWidth / count) / _emWorld;
            _bakeTmp.text = $"<mspace={cellEm.ToString(System.Globalization.CultureInfo.InvariantCulture)}em>{raw}";
            _bakeTmp.ForceMeshUpdate();

            var rt = RenderTexture.GetTemporary(BAKE_TEXT_WIDTH, BAKE_TEXT_HEIGHT, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            _bakeCam.targetTexture = rt;
            _bakeRenderer.enabled = true;
            _bakeCam.Render();
            _bakeRenderer.enabled = false;

            RenderTexture.active = rt;
            var tex = new Texture2D(BAKE_TEXT_WIDTH, BAKE_TEXT_HEIGHT, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, BAKE_TEXT_WIDTH, BAKE_TEXT_HEIGHT), 0, 0);
            tex.Apply();

            RenderTexture.active = prevActive;
            _bakeCam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        public static Texture2D[] BakeText(string[] texts)
        {
            if (texts == null) return new Texture2D[0];
            var result = new Texture2D[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                result[i] = BakeText(texts[i]);
            return result;
        }

        private static void EnsureBaker()
        {
            if (_bakeCam != null && _bakeTmp != null) return;

            var obj = new GameObject("InsanityTextBaker") { hideFlags = HideFlags.HideAndDontSave };
            obj.layer = BAKE_TEXT_LAYER;
            Object.DontDestroyOnLoad(obj);

            _bakeTmp = obj.AddComponent<TextMeshPro>();
            _bakeTmp.font = G.InsanityFont;
            _bakeTmp.color = Color.white;
            _bakeTmp.richText = true;
            _bakeTmp.enableWordWrapping = false;
            _bakeTmp.fontSize = 10f;
            _bakeTmp.alignment = TextAlignmentOptions.Center;

            _bakeRenderer = obj.GetComponent<MeshRenderer>();
            _bakeRenderer.enabled = false;

            var cam = new GameObject("InsanityTextBakerCam") { hideFlags = HideFlags.HideAndDontSave };
            cam.transform.SetParent(obj.transform, false);
            cam.transform.localPosition = new Vector3(0f, 0f, -10f);

            _bakeCam = cam.AddComponent<Camera>();
            _bakeCam.enabled = false;
            _bakeCam.orthographic = true;
            _bakeCam.cullingMask = 1 << BAKE_TEXT_LAYER;
            _bakeCam.clearFlags = CameraClearFlags.SolidColor;
            _bakeCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _bakeCam.nearClipPlane = 0.1f;
            _bakeCam.farClipPlane = 100f;

            _bakeTmp.text = "<mspace=100em>AA";
            _bakeTmp.ForceMeshUpdate();
            var ci = _bakeTmp.textInfo.characterInfo;
            _emWorld = ci.Length >= 2 ? (ci[1].origin - ci[0].origin) / 100f : _bakeTmp.fontSize;

            _bakeTmp.text = "M";
            _bakeTmp.ForceMeshUpdate();
            float lineHeight = _bakeTmp.textBounds.size.y;

            float aspect = (float)BAKE_TEXT_WIDTH / BAKE_TEXT_HEIGHT;
            _bakeCam.aspect = aspect;
            _bakeCam.orthographicSize = lineHeight * 0.5f * 1.1f;
            _worldWidth = _bakeCam.orthographicSize * 2f * aspect;
            _bakeTmp.rectTransform.sizeDelta = new Vector2(_worldWidth, lineHeight * 2f);
        }
    }
}
