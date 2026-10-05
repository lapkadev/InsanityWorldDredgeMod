using UnityEngine;
using InsW.Core;
using Winch.Util;
using static InsW.Core.Constants;
using static InsW.Core.Funcs;

namespace InsW.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksNpc()
        {
            DredgeHooks.RegisterNpc = RegisterNpc;
        }

        public static bool RegisterNpc(string npcId, GameObject portraitPrefab, Sprite icon, string[] dockIds)
        {
            var speakerData = CharacterUtil.GetSpeakerData(npcId);
            if (speakerData == null)
            {
                Log.Warn($"RegisterNpc: npc '{npcId}' not found in CharacterUtil");
                return false;
            }

            ApplyNpcPrefab(speakerData, npcId, portraitPrefab);
            ApplyNpcIcon(speakerData, npcId, icon);

            if (dockIds == null)
            {
                Log.Warn($"RegisterNpc: npc '{npcId}' has no docks listed");
                return true;
            }

            foreach (var dockId in dockIds)
                AddNpcToDock(speakerData, npcId, dockId);

            return true;
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
