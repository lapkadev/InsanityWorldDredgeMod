using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const int BAKE_TEXT_WIDTH = 2048;
        public const int BAKE_TEXT_HEIGHT = 128;
        public const int BAKE_TEXT_LAYER = 31;
    }

    public static partial class G
    {
        public static TextBakerState TextBaker = new TextBakerState();
    }

    public static partial class Funcs
    {
        public static Texture2D BakeText(string text)
        {
            if (G.InsanityFont == null)
            {
                Log.Warn("BakeText: InsanityFont is not loaded");
                return null;
            }

            EnsureBaker();

            var baker = G.TextBaker;
            string raw = text ?? string.Empty;
            int count = Mathf.Max(raw.Length, 1);
            float cellEm = (baker.WorldWidth / count) / baker.EmWorld;
            baker.Text.text = $"<mspace={cellEm.ToString(CultureInfo.InvariantCulture)}em>{raw}";
            baker.Text.ForceMeshUpdate();

            var rt = RenderTexture.GetTemporary(BAKE_TEXT_WIDTH, BAKE_TEXT_HEIGHT, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;

            var gpuProj = GL.GetGPUProjectionMatrix(baker.Cam.projectionMatrix, true);
            var cmd = new CommandBuffer();
            cmd.SetRenderTarget(rt);
            cmd.ClearRenderTarget(true, true, new Color(0f, 0f, 0f, 0f));
            cmd.SetViewProjectionMatrices(baker.Cam.worldToCameraMatrix, gpuProj);
            cmd.DrawMesh(baker.Text.mesh, Matrix4x4.identity, baker.Text.fontSharedMaterial, 0, -1);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();

            RenderTexture.active = rt;
            var tex = new Texture2D(BAKE_TEXT_WIDTH, BAKE_TEXT_HEIGHT, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, BAKE_TEXT_WIDTH, BAKE_TEXT_HEIGHT), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            FlipVertical(tex);
            return tex;
        }

        public static Texture2D[] BakeText(string[] texts)
        {
            if (texts == null)
                return new Texture2D[0];

            var result = new Texture2D[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                result[i] = BakeText(texts[i]);

            return result;
        }

        public static void FlipVertical(Texture2D tex)
        {
            int w = tex.width;
            int h = tex.height;
            var src = tex.GetPixels32();
            var dst = new Color32[src.Length];
            for (int y = 0; y < h; y++)
                System.Array.Copy(src, y * w, dst, (h - 1 - y) * w, w);

            tex.SetPixels32(dst);
            tex.Apply();
        }

        public static void EnsureBaker()
        {
            var baker = G.TextBaker;
            if (baker.Cam != null && baker.Text != null)
                return;

            var obj = new GameObject("InsanityTextBaker") { hideFlags = HideFlags.HideAndDontSave };
            obj.layer = BAKE_TEXT_LAYER;
            Object.DontDestroyOnLoad(obj);

            baker.Text = obj.AddComponent<TextMeshPro>();
            baker.Text.font = G.InsanityFont;
            baker.Text.color = Color.white;
            baker.Text.richText = true;
            baker.Text.enableWordWrapping = false;
            baker.Text.fontSize = 10f;
            baker.Text.alignment = TextAlignmentOptions.Center;

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.enabled = false;

            var cam = new GameObject("InsanityTextBakerCam") { hideFlags = HideFlags.HideAndDontSave };
            cam.transform.SetParent(obj.transform, false);
            cam.transform.localPosition = new Vector3(0f, 0f, -10f);

            baker.Cam = cam.AddComponent<Camera>();
            baker.Cam.enabled = false;
            baker.Cam.orthographic = true;
            baker.Cam.cullingMask = 1 << BAKE_TEXT_LAYER;
            baker.Cam.clearFlags = CameraClearFlags.SolidColor;
            baker.Cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            baker.Cam.nearClipPlane = 0.1f;
            baker.Cam.farClipPlane = 100f;

            baker.Text.text = "<mspace=100em>AA";
            baker.Text.ForceMeshUpdate();
            var ci = baker.Text.textInfo.characterInfo;
            baker.EmWorld = ci.Length >= 2 ? (ci[1].origin - ci[0].origin) / 100f : baker.Text.fontSize;

            baker.Text.text = "M";
            baker.Text.ForceMeshUpdate();
            float lineHeight = baker.Text.textBounds.size.y;

            float aspect = (float)BAKE_TEXT_WIDTH / BAKE_TEXT_HEIGHT;
            baker.Cam.aspect = aspect;
            baker.Cam.orthographicSize = lineHeight * 0.5f * 1.1f;
            baker.WorldWidth = baker.Cam.orthographicSize * 2f * aspect;
            baker.Text.rectTransform.sizeDelta = new Vector2(baker.WorldWidth, lineHeight * 2f);
        }
    }

    public class TextBakerState
    {
        public Camera Cam;
        public TextMeshPro Text;
        public float EmWorld;
        public float WorldWidth;
    }
}
