using UnityEngine.Localization.Components;
using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksText()
        {
            DredgeHooks.UseLocalizedText = (label, key) =>
            {
                var localize = label.GetComponent<LocalizeStringEvent>();
                if (localize == null)
                    localize = label.gameObject.AddComponent<LocalizeStringEvent>();

                localize.SetTable(LanguageManager.STRING_TABLE);
                localize.SetEntry(key);
                localize.OnUpdateString.RemoveAllListeners();
                localize.OnUpdateString.AddListener(text => label.text = text);
                localize.RefreshString();
            };
        }
    }
}
