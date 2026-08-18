using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksSave()
        {
            DredgeHooks.GetActiveSaveSlot = () => G.DredgeGame?.SaveManager?.ActiveSettingsData?.lastSaveSlot ?? -1;
        }
    }
}
