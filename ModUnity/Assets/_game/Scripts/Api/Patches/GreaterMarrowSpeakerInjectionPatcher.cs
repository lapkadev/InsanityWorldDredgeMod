using HarmonyLib;
using InsanityWorldMod.Core;
using Winch.Util;

namespace InsanityWorldMod.Api
{
    [HarmonyPatch(typeof(GameSceneInitializer), nameof(GameSceneInitializer.Start))]
    public static class GreaterMarrowSpeakerInjectionPatcher
    {
        private const string DOCK_ID = "dock.greater-marrow";
        private const string SPEAKER_ID = "lapkadev_mystic";

        [HarmonyPostfix]
        public static void Postfix()
        {
            var dock = DockUtil.GetDock(DOCK_ID);
            if (dock?.Data == null) { G.Log.Warn($"Speaker injection: dock '{DOCK_ID}' not found"); return; }

            var speaker = CharacterUtil.GetSpeakerData(SPEAKER_ID);
            if (speaker == null) { G.Log.Warn($"Speaker injection: speaker '{SPEAKER_ID}' not found in CharacterUtil"); return; }

            if (dock.Data.Speakers.Contains(speaker))
            {
                G.Log.Debug($"Speaker injection: '{SPEAKER_ID}' already in '{DOCK_ID}'.Speakers, skipping");
                return;
            }

            dock.Data.Speakers.Add(speaker);
            G.Log.Info($"Speaker injection: added '{SPEAKER_ID}' to '{DOCK_ID}'.Speakers (count now {dock.Data.Speakers.Count})");
        }
    }
}
