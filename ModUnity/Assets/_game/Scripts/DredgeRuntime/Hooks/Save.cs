using InsW.Core;

namespace InsW.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksSave()
        {
            DredgeHooks.GetActiveSaveSlot = () => G.DredgeGame?.SaveManager?.ActiveSettingsData?.lastSaveSlot ?? -1;
        }
    }
}
