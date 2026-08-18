using InsanityWorldMod.Core;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string DREDGE_FONT_TABLE = "Fonts";
        public const string DREDGE_FONT_ENTRY = "DefaultFont";
    }

    public static partial class Funcs
    {
        public static void AddHooksFont()
        {
            DredgeHooks.UseLocalizedFont = label =>
            {
                var bypass = label.GetComponent<LocalizeFontBypass>();
                if (bypass == null)
                    bypass = label.gameObject.AddComponent<LocalizeFontBypass>();

                bypass.textField = label;
                bypass.tableString = DREDGE_FONT_TABLE;
                bypass.tableEntryString = DREDGE_FONT_ENTRY;
            };
        }
    }
}
