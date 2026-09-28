using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static GameState Game;
    }

    public class GameState
    {
        public float SessionStartTime;
        public bool  IsLoaded;

        private float _insanityLevel;
        public float InsanityLevel
        {
            get => _insanityLevel;
            set => _insanityLevel = Mathf.Clamp(value, 0f, INSANITY_MAX);
        }

        public void InitFromSave()
        {
            SessionStartTime = Time.time;
            InsanityLevel = G.Save.InsanityLevel ?? INSANITY_BASE_MAX - GetSanity();
            IsLoaded = true;
        }

        public void CaptureFromDredge()
        {
            if (!IsLoaded)
                return;

            G.Save.InsanityLevel = InsanityLevel;
        }

        public void ApplyToDredge()
        {
            // Placeholder: push G.Save state back into Dredge runtime.
        }
    }
}
