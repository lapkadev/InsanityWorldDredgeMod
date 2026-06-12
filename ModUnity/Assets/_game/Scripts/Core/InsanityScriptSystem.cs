using System.Text;
using TMPro;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string SCRIPT_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const string SCRIPT_GLYPHS = SCRIPT_ALPHABET + "/\\.,:;-+=!?()%";

        public const string FONT_INSANITY = "sdf_insanity_font";
    }

    public static partial class G
    {
        public static TMP_FontAsset InsanityFont { get; set; }
    }

    public static partial class Funcs
    {
        public static void InitInsanityScript()
        {
            G.InsanityFont = G.Fonts[FONT_INSANITY];
        }

        public static string Vig(string text, string key)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string k = VigSanitizeKey(key);
            if (k.Length == 0) return text.ToUpperInvariant();

            int n = SCRIPT_ALPHABET.Length;
            var sb = new StringBuilder(text.Length);
            int ki = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = char.ToUpperInvariant(text[i]);
                int idx = SCRIPT_ALPHABET.IndexOf(ch);
                if (idx >= 0)
                {
                    int shift = SCRIPT_ALPHABET.IndexOf(k[ki % k.Length]) + 1;
                    sb.Append(SCRIPT_ALPHABET[(idx + shift) % n]);
                    ki++;
                }
                else
                {
                    sb.Append(text[i]);
                }
            }
            return sb.ToString();
        }

        private static string VigSanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string up = key.ToUpperInvariant();
            var sb = new StringBuilder(up.Length);
            for (int i = 0; i < up.Length; i++)
                if (SCRIPT_ALPHABET.IndexOf(up[i]) >= 0) sb.Append(up[i]);
            return sb.ToString();
        }
    }

    public class InsanityScriptSystem : IInsanityWorldSystem
    {
        public int Order => 9;

        public void OnLoad()
        {
            InitInsanityScript();
        }
    }
}
