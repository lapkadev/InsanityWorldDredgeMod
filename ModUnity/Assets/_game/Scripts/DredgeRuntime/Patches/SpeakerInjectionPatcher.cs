using HarmonyLib;
using InsanityWorldMod.Core;
using Winch.Util;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    [HarmonyPatch(typeof(GameSceneInitializer), nameof(GameSceneInitializer.Start))]
    public static class SpeakerInjectionPatcher
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            foreach (var injection in SPEAKER_INJECTIONS)
                InjectSpeaker(injection.DockId, injection.SpeakerId);
        }

        private static void InjectSpeaker(string dockId, string speakerId)
        {
            var dock = DockUtil.GetDock(dockId);
            if (dock?.Data == null)
            {
                Log.Warn($"Speaker injection: dock '{dockId}' not found");
                return;
            }

            var speaker = CharacterUtil.GetSpeakerData(speakerId);
            if (speaker == null)
            {
                Log.Warn($"Speaker injection: speaker '{speakerId}' not found in CharacterUtil");
                return;
            }

            if (dock.Data.Speakers.Contains(speaker))
            {
                Log.Debug($"Speaker injection: '{speakerId}' already in '{dockId}'.Speakers, skipping");
                return;
            }

            dock.Data.Speakers.Add(speaker);
            Log.Info($"Speaker injection: added '{speakerId}' to '{dockId}'.Speakers (count now {dock.Data.Speakers.Count})");
        }
    }
}
