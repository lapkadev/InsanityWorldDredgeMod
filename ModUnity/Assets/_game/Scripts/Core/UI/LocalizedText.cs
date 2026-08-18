using TMPro;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static void UseLocalizedText(this TextMeshProUGUI label, string key)
        {
            DredgeHooks.UseLocalizedText(label, key);
        }
    }
}
