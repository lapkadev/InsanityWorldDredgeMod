using InsanityWorldMod.Core;
using Winch.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksLog()
        {
            Log.Info  = msg => WinchCore.Log.Info(msg);
            Log.Warn  = msg => WinchCore.Log.Warn(msg);
            Log.Error = msg => WinchCore.Log.Error(msg);
            Log.Debug = msg => WinchCore.Log.Debug(msg);
        }
    }
}
