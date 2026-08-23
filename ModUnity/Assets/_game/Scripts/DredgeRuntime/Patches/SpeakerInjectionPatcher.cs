using HarmonyLib;
using InsanityWorldMod.Core;
using Winch.Util;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Funcs;
using Core = InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    [HarmonyPatch(typeof(GameSceneInitializer), nameof(GameSceneInitializer.Start))]
    public static class SpeakerInjectionPatcher
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            foreach (var injection in SPEAKER_INJECTIONS)
                InjectSpeaker(injection);
        }

        private static void InjectSpeaker(SpeakerInjection injection)
        {
            var dock = DockUtil.GetDock(injection.DockId);
            if (dock?.Data == null)
            {
                Log.Warn($"Speaker injection: dock '{injection.DockId}' not found");
                return;
            }

            var speaker = CharacterUtil.GetSpeakerData(injection.SpeakerId);
            if (speaker == null)
            {
                Log.Warn($"Speaker injection: speaker '{injection.SpeakerId}' not found in CharacterUtil");
                return;
            }

            ApplyPrefab(speaker, injection.SpeakerId, injection.PrefabName);
            ApplyIcon(speaker, injection.SpeakerId, injection.IconName);
            CreateNpcCameras(injection.CameraPrefabName);

            if (dock.Data.Speakers.Contains(speaker))
            {
                Log.Debug($"Speaker injection: '{injection.SpeakerId}' already in '{injection.DockId}'.Speakers, skipping");
                return;
            }

            dock.Data.Speakers.Add(speaker);
            Log.Info($"Speaker injection: added '{injection.SpeakerId}' to '{injection.DockId}'.Speakers (count now {dock.Data.Speakers.Count})");
        }

        private static void ApplyIcon(SpeakerData speaker, string speakerId, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return;

            if (!Core.G.Sprites.TryGetValue(spriteName, out var sprite) || sprite == null)
            {
                Log.Warn($"Speaker injection: sprite '{spriteName}' not found among loaded bundles");
                return;
            }

            if (speaker.smallPortraitSprite == sprite)
                return;

            speaker.smallPortraitSprite = sprite;
            Log.Info($"Speaker injection: sprite '{spriteName}' assigned to '{speakerId}'");
        }

        private static void ApplyPrefab(SpeakerData speaker, string speakerId, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return;

            if (!Core.G.Prefabs.TryGetValue(prefabName, out var prefab) || prefab == null)
            {
                Log.Warn($"Speaker injection: prefab '{prefabName}' not found among loaded bundles");
                return;
            }

            if (speaker.portraitPrefab == prefab)
                return;

            speaker.portraitPrefab = prefab;
            Log.Info($"Speaker injection: prefab '{prefabName}' assigned to '{speakerId}'");
        }
    }
}
