using System;

namespace InsanityWorldMod.Core
{
    public static partial class Funcs
    {
        public static void OnDeath()
        {
            if (G.Save == null)
            {
                Log.Warn("OnDeath: save state is null, death not counted");
                return;
            }

            G.Save.Stats.Deaths++;
            Log.Info($"OnDeath: deaths {G.Save.Stats.Deaths}");
            Save();
        }
    }

    [Serializable]
    public class Statistics
    {
        public int Deaths;
    }
}
