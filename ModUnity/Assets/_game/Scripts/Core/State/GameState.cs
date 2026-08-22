using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static GameState Game;
    }

    public class GameState
    {
        public float SessionStartTime;
        public bool IsRestartInProgress;

        public void InitFromSave()
        {
            SessionStartTime = Time.time;
        }

        public void CaptureFromDredge()
        {
            // Placeholder: pull Dredge-runtime state we care about into G.Save.
        }

        public void ApplyToDredge()
        {
            // Placeholder: push G.Save state back into Dredge runtime.
        }
    }
}
