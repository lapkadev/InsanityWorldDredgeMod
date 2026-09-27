using System.Collections.Generic;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string MARK_BLIP_NAME            = "MarkBlip";
        public const string MARK_OUTLINE_NAME         = "MarkOutline";
        public const string MARK_OUTLINE_LAYER_SUFFIX = "Outlines";
        public const int    MARK_LAYER_LOOT           = 0;
        public const int    MARK_LAYER_FISH           = 1;
        public const int    MARK_LAYER_PLAYER_ITEM    = 2;
        public const int    MARK_LAYER_THREAT         = 3;

        public static readonly string[] MARK_LAYER_NAMES = { "LootMarks", "FishMarks", "PlayerItemMarks", "ThreatMarks" };

        public const float MARK_SIZE_SMALL_MONSTER_PX  = 6f;
        public const float MARK_SIZE_MEDIUM_MONSTER_PX = 12f;
        public const float MARK_SIZE_BIG_MONSTER_PX    = 18f;
        public const float MARK_SIZE_FISH_PX           = 6f;
        public const float MARK_SIZE_LOOT_PX           = 6f;
        public const float MARK_SIZE_PLAYER_ITEM_PX    = 6f;

        public const float COMPASS_FULL_SPEED             = 15f;
        public const float RESOURCE_COMPASS_CLEAR_RANGE_M = 50f;
        public const float RESOURCE_COMPASS_RANGE_M       = 200f;
        public const float RESOURCE_CLARITY_AT_FULL_SPEED = 0.3f;
        public const float THREAT_COMPASS_CLEAR_RANGE_M   = 75f;
        public const float THREAT_COMPASS_RANGE_M         = 300f;
        public const float THREAT_CLARITY_AT_FULL_SPEED   = 0.65f;

        public const float THREAT_SCAN_INTERVAL_SEC = 0.1f;
        public const float THREAT_BLINK_RATE        = 2.6f;
        public const float THREAT_MIN_ALPHA         = 0.15f;
        public const float RESOURCE_BLINK_RATE      = 0.8f;

        public static readonly Color THREAT_BLIP_COLOR      = new Color(0.92f, 0.12f, 0.10f, 1f);
        public static readonly Color FISH_BLIP_COLOR        = new Color(0.30f, 0.55f, 0.80f, 1f);
        public static readonly Color LOOT_BLIP_COLOR        = new Color(0.46f, 0.46f, 0.46f, 1f);
        public static readonly Color PLAYER_ITEM_BLIP_COLOR = new Color(0.40f, 0.72f, 0.30f, 1f);

        public const float MARK_OUTLINE_WIDTH_PX = 1f;

        public static readonly Color THREAT_OUTLINE_COLOR      = new Color(0.96f, 0.24f, 0.20f, 1f);
        public static readonly Color FISH_OUTLINE_COLOR        = new Color(0.40f, 0.66f, 0.90f, 1f);
        public static readonly Color LOOT_OUTLINE_COLOR        = new Color(0.70f, 0.70f, 0.70f, 1f);
        public static readonly Color PLAYER_ITEM_OUTLINE_COLOR = new Color(0.50f, 0.82f, 0.40f, 1f);
    }

    public static partial class G
    {
        public static readonly List<MinimapMark> Threats = new List<MinimapMark>();
    }

    public static partial class Funcs
    {
        public static void RefreshThreats()
        {
            G.Threats.Clear();
            CollectThreats(G.Threats);
        }

        public static float GetThreatBlipAlpha()
        {
            return GetBlipAlpha(THREAT_BLINK_RATE);
        }

        public static float GetResourceBlipAlpha()
        {
            return GetBlipAlpha(RESOURCE_BLINK_RATE);
        }

        public static float GetMarkLayerBlinkAlpha(int layer)
        {
            return layer == MARK_LAYER_THREAT ? GetThreatBlipAlpha() : GetResourceBlipAlpha();
        }

        public static int GetMinimapMarkLayer(MinimapMarkKind kind)
        {
            switch (kind)
            {
                case MinimapMarkKind.Loot:       return MARK_LAYER_LOOT;
                case MinimapMarkKind.Fish:       return MARK_LAYER_FISH;
                case MinimapMarkKind.PlayerItem: return MARK_LAYER_PLAYER_ITEM;
                default:                         return MARK_LAYER_THREAT;
            }
        }

        public static float GetBlipAlpha(float rate)
        {
            float wave = Mathf.PingPong(Time.unscaledTime * rate, 1f);
            return Mathf.Lerp(THREAT_MIN_ALPHA, 1f, wave);
        }

        public static float GetCompassRangeAlpha(float distanceM, MinimapMarkKind kind)
        {
            if (IsResourceMark(kind))
                return 1f - Mathf.InverseLerp(RESOURCE_COMPASS_CLEAR_RANGE_M, RESOURCE_COMPASS_RANGE_M, distanceM);

            return 1f - Mathf.InverseLerp(THREAT_COMPASS_CLEAR_RANGE_M, THREAT_COMPASS_RANGE_M, distanceM);
        }

        public static float GetCompassClarity(float speed, MinimapMarkKind kind)
        {
            float atFullSpeed = IsResourceMark(kind) ? RESOURCE_CLARITY_AT_FULL_SPEED : THREAT_CLARITY_AT_FULL_SPEED;
            return Mathf.Lerp(1f, atFullSpeed, Mathf.InverseLerp(0f, COMPASS_FULL_SPEED, speed));
        }

        public static float GetMinimapMarkSizePx(MinimapMarkKind kind)
        {
            switch (kind)
            {
                case MinimapMarkKind.MediumMonster: return MARK_SIZE_MEDIUM_MONSTER_PX;
                case MinimapMarkKind.BigMonster:    return MARK_SIZE_BIG_MONSTER_PX;
                case MinimapMarkKind.Fish:          return MARK_SIZE_FISH_PX;
                case MinimapMarkKind.Loot:          return MARK_SIZE_LOOT_PX;
                case MinimapMarkKind.PlayerItem:    return MARK_SIZE_PLAYER_ITEM_PX;
                default:                            return MARK_SIZE_SMALL_MONSTER_PX;
            }
        }

        public static Color GetMinimapMarkColor(MinimapMarkKind kind)
        {
            switch (kind)
            {
                case MinimapMarkKind.Fish:       return FISH_BLIP_COLOR;
                case MinimapMarkKind.Loot:       return LOOT_BLIP_COLOR;
                case MinimapMarkKind.PlayerItem: return PLAYER_ITEM_BLIP_COLOR;
                default:                         return THREAT_BLIP_COLOR;
            }
        }

        public static Color GetMinimapMarkOutlineColor(MinimapMarkKind kind)
        {
            switch (kind)
            {
                case MinimapMarkKind.Fish:       return FISH_OUTLINE_COLOR;
                case MinimapMarkKind.Loot:       return LOOT_OUTLINE_COLOR;
                case MinimapMarkKind.PlayerItem: return PLAYER_ITEM_OUTLINE_COLOR;
                default:                         return THREAT_OUTLINE_COLOR;
            }
        }

        public static bool IsResourceMark(MinimapMarkKind kind)
        {
            return kind == MinimapMarkKind.Fish || kind == MinimapMarkKind.Loot || kind == MinimapMarkKind.PlayerItem;
        }
    }

    public enum MinimapMarkKind
    {
        SmallMonster,
        MediumMonster,
        BigMonster,
        Fish,
        Loot,
        PlayerItem,
    }

    public struct MinimapMark
    {
        public Transform Node;
        public MinimapMarkKind Kind;

        public MinimapMark(Transform node, MinimapMarkKind kind)
        {
            Node = node;
            Kind = kind;
        }
    }
}
