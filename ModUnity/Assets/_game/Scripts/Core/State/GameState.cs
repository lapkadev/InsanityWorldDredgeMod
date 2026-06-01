using UnityEngine;

namespace InsanityWorldMod.Core
{
    public class GameState
    {
        public float SessionStartTime;
        public bool IsRestartInProgress;

        public void InitFromSave()
        {
            SessionStartTime = Time.time;
        }

        public void CaptureFromVanilla()
        {
            // Placeholder: pull vanilla-runtime state we care about into G.Save.
        }

        public void ApplyToVanilla()
        {
            // Placeholder: push G.Save state back into vanilla runtime.
        }
    }
}
