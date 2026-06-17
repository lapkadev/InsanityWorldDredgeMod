using HarmonyLib;
using InsanityWorldMod.Core;
using Winch.Util;
using static InsanityWorldMod.Api.Constants;

namespace InsanityWorldMod.Api
{
    public static partial class Constants
    {
        public const string GREATER_MARROW_DOCK_ID = "dock.greater-marrow";
        public const string MYSTIC_SPEAKER_ID      = "lapkadev_mystic";
    }

    [HarmonyPatch(typeof(GameSceneInitializer), nameof(GameSceneInitializer.Start))]
    public static class GreaterMarrowSpeakerInjectionPatcher
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            var dock = DockUtil.GetDock(GREATER_MARROW_DOCK_ID);
            if (dock?.Data == null) { G.Log.Warn($"Speaker injection: dock '{GREATER_MARROW_DOCK_ID}' not found"); return; }

            var speaker = CharacterUtil.GetSpeakerData(MYSTIC_SPEAKER_ID);
            if (speaker == null) { G.Log.Warn($"Speaker injection: speaker '{MYSTIC_SPEAKER_ID}' not found in CharacterUtil"); return; }

            if (dock.Data.Speakers.Contains(speaker))
            {
                G.Log.Debug($"Speaker injection: '{MYSTIC_SPEAKER_ID}' already in '{GREATER_MARROW_DOCK_ID}'.Speakers, skipping");
                return;
            }

            dock.Data.Speakers.Add(speaker);
            G.Log.Info($"Speaker injection: added '{MYSTIC_SPEAKER_ID}' to '{GREATER_MARROW_DOCK_ID}'.Speakers (count now {dock.Data.Speakers.Count})");
        }
    }
}
