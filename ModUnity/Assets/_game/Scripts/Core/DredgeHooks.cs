using System;
using System.Collections.Generic;
using Cinemachine;
using InControl;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace InsW.Core
{
    /// <summary>
    /// Delegates for DREDGE / Winch APIs that Core cannot reference at compile time.
    /// Core.asmdef intentionally does NOT reference Winch - keeps the DLL boundary clean
    /// (Core = pure gameplay, DredgeRuntime = mod-loader glue).
    /// DredgeRuntime MUST add these at <c>EntrySystem.OnLoad()</c> before any Core code runs.
    ///
    /// Pattern: dependency inversion - Core declares the contract, DredgeRuntime supplies
    /// the implementation. No defaults: if a hook is not added, calling it throws
    /// NullReferenceException - surfacing the missing hook loudly rather than silently no-op'ing.
    /// </summary>
    public static class DredgeHooks
    {
        public static Func<bool> IsPlayerSailing;

        public static Func<DialogueRunner> GetDialogueRunner;

        public static Func<string, GameObject, Sprite, string[], bool> RegisterNpc;

        public static Action<string, string, CinemachineVirtualCamera> RegisterNpcCamera;

        public static Func<string, bool> IsDialogueNodeVisited;

        public static Action<string, bool> SetDialogueNodeVisited;

        public static Action<bool> SetQuestGridHelpVisible;

        public static Action<NotificationKind, string, NotificationColor> ShowNotification;

        public static Action RepairHullAll;

        public static Action RepairItemsDurability;

        public static Action<int> RepairHull;

        public static Action<int> DamageHull;

        public static Func<string[]> GetRegularFishIds;

        public static Func<string[]> GetAberrationFishIds;

        public static Func<string, bool> GiveFishToPlayer;

        public static Func<string, bool> GiveItemToPlayer;

        public static Func<string[]> GetEngineIds;

        public static Func<string, float> GetEngineSpeed;

        public static Func<string, string> GetItemShape;

        public static Func<string, string> GetItemName;

        public static Func<string[]> GetAbilityIds;

        public static Func<string, bool> IsAbilityUnlocked;

        public static Action<string, bool> SetAbilityUnlocked;

        public static Func<int> GetActiveSaveSlot;

        public static Func<DockSlot?> GetLastDock;

        public static Func<string[]> GetDockIds;

        public static Func<string, string> GetDockName;

        public static Func<string, int, bool> TeleportShipToDock;

        public static Func<Vector3, bool> TeleportShipTo;

        public static Action CancelPendingTeleport;

        public static Func<Transform> GetPlayerTransform;

        public static Action<List<MinimapMark>> CollectThreats;

        public static Action<GameObject> MakeSolid;

        public static Func<Transform, bool> AttachRelicParticles;

        public static Action<bool> SetBoundaryGuardEnabled;

        public static Func<string, bool> StartWorldEvent;

        public static Func<float> GetSanity;

        public static Action<float> ChangeSanity;

        public static Func<float> GetSanityFrameDelta;

        public static Func<bool> IsInGame;

        public static Action<TextMeshProUGUI> UseLocalizedFont;

        public static Action<TextMeshProUGUI, string> UseLocalizedText;

        public static Action<GameObject, Action> SetMenuButtonClick;

        public static Func<RectTransform> GetModsMenuButton;

        public static Func<Action, bool, int> AddInputBackAction;

        public static Action<int> RemoveInputBackAction;

        public static Action HideUnpausePrompt;

        public static Func<string, GameObject> CreateSettingsClone;

        public static Func<GameObject, string[], RectTransform[]> SetSettingsTabs;

        public static Action<GameObject> ShowSettings;

        public static Action<GameObject> HideSettings;

        public static Action<GameObject, Action> SetSettingsCloseHandler;

        public static Func<RectTransform> CreateMapClone;

        public static Func<float> GetMapPixelsPerWorldUnit;

        public static Func<TMP_FontAsset> GetDredgeCompassFont;

        public static Func<float> GetDredgeCompassFontSize;

        public static Action<float> ShiftHudTabBelow;

        public static Func<PlayerAction, bool, Sprite> GetActionIcon;

        public static Action<Action<BindingSourceType, InputDeviceStyle>> SubscribeInputChanged;

        public static Action<Action<BindingSourceType, InputDeviceStyle>> UnsubscribeInputChanged;
    }

    public struct DockSlot
    {
        public string DockId;
        public int SlotIndex;
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

    public enum NotificationKind
    {
        NONE,
        MONEY_GAINED,
        MONEY_LOST,
        BOOK_ADDED,
        BOOK_COMPLETED,
        ITEM_ADDED,
        ITEM_REMOVED,
        ERROR,
        SPOOKY_EVENT,
        QUEST_STARTED,
        QUEST_UPDATED,
        QUEST_COMPLETED,
        EQUIPMENT_DAMAGED,
        EQUIPMENT_REPAIRED,
        DURABILITY_LOST,
        CRAB_POT_DEPLOYED,
        DAMAGE_TAKEN,
        ITEM_HANDED_IN,
        DEBT_REPAID,
        ROT,
        TELEPORT_ANCHOR_PLACED,
        TELEPORT_ANCHOR_RETRIEVED,
        DARK_SPLASH_ADDED,
        ANY_REPAIR_KIT_USED,
    }

    public enum NotificationColor
    {
        NEUTRAL,
        EMPHASIS,
        POSITIVE,
        NEGATIVE,
        CRITICAL,
        WARNING,
        VALUABLE,
        DISABLED,
    }
}
