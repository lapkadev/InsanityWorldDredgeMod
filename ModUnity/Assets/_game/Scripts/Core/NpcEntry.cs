using System;
using UnityEngine;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string PFB_NPC_ARRAY = "pfb_npc_array";
    }

    public static partial class Funcs
    {
        public static void ValidateNpc(NpcEntry entry)
        {
            if (entry == null || entry.NpcCameras == null)
                return;

            foreach (var item in entry.NpcCameras.GetComponentsInChildren<NpcCamera>(true))
            {
                if (string.IsNullOrEmpty(item.DockId))
                {
                    Log.Warn($"ValidateNpc: '{entry.NpcId}' has camera '{item.name}' without a dock id");
                    continue;
                }

                if (entry.DockIds == null || Array.IndexOf(entry.DockIds, item.DockId) < 0)
                    Log.Warn($"ValidateNpc: '{entry.NpcId}' camera '{item.name}' targets dock '{item.DockId}' missing from DockIds");
            }
        }
    }

    public class NpcEntry : MonoBehaviour
    {
        public TextAsset NpcJson;
        public string[] DockIds;
        public GameObject Npc;
        public GameObject NpcCameras;
        public Sprite Icon;

        public string NpcId => NpcJson != null ? NpcJson.name : null;
    }
}
