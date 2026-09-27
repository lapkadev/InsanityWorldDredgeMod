using UnityEngine;
using InsanityWorldMod.Core;
using Winch.Util;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Funcs;
using Core = InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void InjectNpcs()
        {
            if (!Core.G.Prefabs.TryGetValue(PFB_NPC_ARRAY, out var prefab) || prefab == null)
            {
                Log.Warn($"InjectNpcs: prefab '{PFB_NPC_ARRAY}' not found among loaded bundles");
                return;
            }

            var entries = prefab.GetComponentsInChildren<NpcEntry>(true);
            if (entries.Length == 0)
            {
                Log.Warn($"InjectNpcs: prefab '{PFB_NPC_ARRAY}' contains no NpcEntry components");
                return;
            }

            Log.Info($"InjectNpcs: {entries.Length} npc entry(ies) found");

            foreach (var entry in entries)
                InjectNpc(entry);
        }

        public static void InjectNpc(NpcEntry entry)
        {
            if (entry == null)
                return;

            if (string.IsNullOrEmpty(entry.NpcId))
            {
                Log.Warn("InjectNpc: entry has no npc json assigned");
                return;
            }

            var speakerData = CharacterUtil.GetSpeakerData(entry.NpcId);
            if (speakerData == null)
            {
                Log.Warn($"InjectNpc: npc '{entry.NpcId}' not found in CharacterUtil");
                return;
            }

            ValidateNpc(entry);

            ApplyNpcPrefab(speakerData, entry.NpcId, entry.Npc);
            ApplyNpcIcon(speakerData, entry.NpcId, entry.Icon);
            CreateNpcCameras(entry.NpcCameras, entry.NpcId);

            if (entry.DockIds == null)
            {
                Log.Warn($"InjectNpc: npc '{entry.NpcId}' has no docks listed");
                return;
            }

            foreach (var dockId in entry.DockIds)
                AddNpcToDock(speakerData, entry.NpcId, dockId);
        }

        public static void AddNpcToDock(SpeakerData speakerData, string npcId, string dockId)
        {
            var dock = DockUtil.GetDock(dockId);
            if (dock?.Data == null)
            {
                Log.Warn($"AddNpcToDock: dock '{dockId}' not found");
                return;
            }

            if (dock.Data.Speakers.Contains(speakerData))
            {
                Log.Debug($"AddNpcToDock: '{npcId}' already in '{dockId}'.Speakers, skipping");
                return;
            }

            dock.Data.Speakers.Add(speakerData);
            Log.Info($"AddNpcToDock: added '{npcId}' to '{dockId}'.Speakers (count now {dock.Data.Speakers.Count})");
        }

        public static void ApplyNpcIcon(SpeakerData speakerData, string npcId, Sprite sprite)
        {
            if (sprite == null || speakerData.smallPortraitSprite == sprite)
                return;

            speakerData.smallPortraitSprite = sprite;
            Log.Info($"ApplyNpcIcon: sprite '{sprite.name}' assigned to '{npcId}'");
        }

        public static void ApplyNpcPrefab(SpeakerData speakerData, string npcId, GameObject prefab)
        {
            if (prefab == null || speakerData.portraitPrefab == prefab)
                return;

            speakerData.portraitPrefab = prefab;
            Log.Info($"ApplyNpcPrefab: prefab '{prefab.name}' assigned to '{npcId}'");
        }
    }
}
