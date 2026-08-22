namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static RunState Run;
    }

    public static partial class Funcs
    {
        public static void StartNewRun()
        {
            G.Run = new RunState();
            if (G.Save != null) G.Save.TotalRuns++;
            Log.Info($"StartNewRun: run #{G.Save?.TotalRuns}");
        }

        public static void OnDeathIntercepted()
        {
            if (G.Run == null) { StartNewRun(); return; }
            if (G.Save != null) G.Save.TotalDeathsIntercepted++;

            Save();
            StartNewRun();
        }
    }

    public class RunState
    {
        public float SecondsSinceRunStart;
        public int ItemsCollectedThisRun;

        public void Tick(float deltaTime)
        {
            SecondsSinceRunStart += deltaTime;
        }
    }
}
