using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using InsanityWorldMod.Core;
using UnityEngine;
using static InsanityWorldMod.DredgeRuntime.Constants;
using static InsanityWorldMod.DredgeRuntime.Funcs;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public static readonly (Type Type, MinimapMarkKind Kind)[] THREAT_TYPES =
        {
            (typeof(MarrowMonster),            MinimapMarkKind.SmallMonster),
            (typeof(GCMonster),                MinimapMarkKind.BigMonster),
            (typeof(IceMonster),               MinimapMarkKind.MediumMonster),
            (typeof(TSMonster),                MinimapMarkKind.SmallMonster),
            (typeof(DSBigMonster),             MinimapMarkKind.MediumMonster),
            (typeof(DSLittleMonster),          MinimapMarkKind.SmallMonster),
            (typeof(OozeMonster),              MinimapMarkKind.MediumMonster),
            (typeof(OozeEvent),                MinimapMarkKind.SmallMonster),
            (typeof(WreckMonster),             MinimapMarkKind.MediumMonster),
            (typeof(SBMonsterAnimationHelper), MinimapMarkKind.BigMonster),
            (typeof(WaterspoutWorldEvent),     MinimapMarkKind.SmallMonster),
            (typeof(VinesWorldEvent),          MinimapMarkKind.SmallMonster),
            (typeof(LeviathanWorldEvent),      MinimapMarkKind.BigMonster),
            (typeof(ParasiteWorldEvent),       MinimapMarkKind.SmallMonster),
            (typeof(MonsterRayWorldEvent),     MinimapMarkKind.MediumMonster),
            (typeof(PhantomSharkWorldEvent),   MinimapMarkKind.MediumMonster),
        };

        public const string FOG_DEVIL_STATE_FIELD   = "currentState";
        public const string FOG_DEVIL_SPAWNED_STATE = "SPAWNED";
        public const string JELLYFISH_BODY_FIELD    = "bodyTransform";
        public const string JELLYFISH_IS_UP_FIELD   = "isUp";

        public static readonly Action<List<MinimapMark>>[] THREAT_EXTRA_SOURCES =
        {
            CollectFogDevilThreats,
            CollectJellyfishThreats,
            CollectHarvestPoiMarks,
            CollectItemPoiMarks,
        };
    }

    public static partial class G
    {
        internal static ThreatScanState ThreatScan = new ThreatScanState();
    }

    public static partial class Funcs
    {
        public static void AddHooksThreats()
        {
            DredgeHooks.CollectThreats = target =>
            {
                if (target == null)
                    return;

                ScanNextThreatSource();
                MergeThreatCaches(target, G.ThreatScan.Seen);
                CollectCurrentWorldEventThreat(target, G.ThreatScan.Seen);
            };
        }

        public static void ScanNextThreatSource()
        {
            var scan = G.ThreatScan;
            if (scan.Caches == null)
            {
                scan.Caches = new List<MinimapMark>[THREAT_TYPES.Length + THREAT_EXTRA_SOURCES.Length];
                for (int i = 0; i < scan.Caches.Length; i++)
                    scan.Caches[i] = new List<MinimapMark>();
            }

            int index = scan.NextSource;
            scan.NextSource = (index + 1) % scan.Caches.Length;

            var cache = scan.Caches[index];
            cache.Clear();

            if (index < THREAT_TYPES.Length)
                CollectThreatsOfType(THREAT_TYPES[index].Type, THREAT_TYPES[index].Kind, cache);
            else
                THREAT_EXTRA_SOURCES[index - THREAT_TYPES.Length](cache);
        }

        public static void MergeThreatCaches(List<MinimapMark> target, HashSet<Transform> seen)
        {
            seen.Clear();

            var caches = G.ThreatScan.Caches;
            for (int i = 0; i < caches.Length; i++)
            {
                var cache = caches[i];
                for (int j = 0; j < cache.Count; j++)
                {
                    var mark = cache[j];
                    if (mark.Node != null && seen.Add(mark.Node))
                        target.Add(mark);
                }
            }
        }

        public static void CollectThreatsOfType(Type type, MinimapMarkKind kind, List<MinimapMark> target)
        {
            foreach (var found in UnityEngine.Object.FindObjectsOfType(type))
            {
                var behaviour = found as MonoBehaviour;
                if (behaviour != null)
                    target.Add(new MinimapMark(behaviour.transform, kind));
            }
        }

        public static void CollectFogDevilThreats(List<MinimapMark> target)
        {
            var scan = G.ThreatScan;
            if (scan.FogDevilState == null)
                scan.FogDevilState = AccessTools.Field(typeof(FogDevil), FOG_DEVIL_STATE_FIELD);

            if (scan.FogDevilState == null)
                return;

            foreach (var fogDevil in UnityEngine.Object.FindObjectsOfType<FogDevil>())
            {
                var state = scan.FogDevilState.GetValue(fogDevil);
                if (state != null && state.ToString() == FOG_DEVIL_SPAWNED_STATE)
                    target.Add(new MinimapMark(fogDevil.transform, MinimapMarkKind.SmallMonster));
            }
        }

        public static void CollectJellyfishThreats(List<MinimapMark> target)
        {
            var scan = G.ThreatScan;
            if (scan.JellyfishIsUp == null)
                scan.JellyfishIsUp = AccessTools.FieldRefAccess<Jellyfish, bool>(JELLYFISH_IS_UP_FIELD);

            if (scan.JellyfishBody == null)
                scan.JellyfishBody = AccessTools.FieldRefAccess<Jellyfish, Transform>(JELLYFISH_BODY_FIELD);

            foreach (var jellyfish in UnityEngine.Object.FindObjectsOfType<Jellyfish>())
            {
                if (!scan.JellyfishIsUp(jellyfish))
                    continue;

                var body = scan.JellyfishBody(jellyfish);
                target.Add(new MinimapMark(body != null ? body : jellyfish.transform, MinimapMarkKind.SmallMonster));
            }
        }

        public static void CollectHarvestPoiMarks(List<MinimapMark> target)
        {
            foreach (var poi in UnityEngine.Object.FindObjectsOfType<HarvestPOI>(true))
            {
                if (poi is PlacedHarvestPOI)
                {
                    target.Add(new MinimapMark(poi.transform, MinimapMarkKind.PlayerItem));
                    continue;
                }

                var harvestable = poi.Harvestable;
                if (harvestable == null || poi.IsCrabPotPOI)
                    continue;

                if (harvestable.IsHarvestable() != HarvestQueryEnum.VALID)
                    continue;

                var kind = harvestable.GetHarvestType() == HarvestableType.DREDGE ? MinimapMarkKind.Loot : MinimapMarkKind.Fish;
                target.Add(new MinimapMark(poi.transform, kind));
            }
        }

        public static void CollectItemPoiMarks(List<MinimapMark> target)
        {
            foreach (var poi in UnityEngine.Object.FindObjectsOfType<ItemPOI>(true))
            {
                var harvestable = poi.Harvestable;
                if (harvestable != null && harvestable.IsHarvestable() == HarvestQueryEnum.VALID)
                    target.Add(new MinimapMark(poi.transform, MinimapMarkKind.Loot));
            }
        }

        public static void CollectCurrentWorldEventThreat(List<MinimapMark> target, HashSet<Transform> seen)
        {
            var current = G.DredgeGame?.WorldEventManager?.CurrentEvent;
            if (current == null)
                return;

            if (current.worldEventData == null || !current.worldEventData.dispelByBanish)
                return;

            if (seen.Add(current.transform))
                target.Add(new MinimapMark(current.transform, MinimapMarkKind.SmallMonster));
        }
    }

    internal class ThreatScanState
    {
        public List<MinimapMark>[] Caches;
        public int NextSource;
        public readonly HashSet<Transform> Seen = new HashSet<Transform>();
        public FieldInfo FogDevilState;
        public AccessTools.FieldRef<Jellyfish, bool> JellyfishIsUp;
        public AccessTools.FieldRef<Jellyfish, Transform> JellyfishBody;
    }
}
