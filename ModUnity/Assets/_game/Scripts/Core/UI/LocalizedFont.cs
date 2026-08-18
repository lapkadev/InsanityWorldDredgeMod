using TMPro;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static void UseLocalizedFont(this TextMeshProUGUI label)
        {
            DredgeHooks.UseLocalizedFont(label);
        }
    }
}
