using System;
using System.Collections.Generic;
using InControl;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace InsanityWorldMod.Core
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
        /// <summary>
        /// Returns every AssetBundle Winch has loaded.
        /// </summary>
        public static Func<IEnumerable<AssetBundle>> GetAllBundles;

        public static Func<bool> IsPlayerSailing;

        public static Func<DialogueRunner> GetDialogueRunner;

        public static Action<NotificationKind, string, NotificationColor> ShowNotification;

        public static Action RepairHull;

        public static Action RepairAllItems;

        public static Func<int> GetActiveSaveSlot;

        public static Func<DockSlot?> GetLastDock;

        public static Func<string, int, bool> MoveShipToDock;

        public static Action CancelPendingTeleport;

        public static Func<Transform> GetPlayerTransform;

        public static Func<bool> IsInGame;

        public static Action<TextMeshProUGUI> UseLocalizedFont;

        public static Action<TextMeshProUGUI, string> UseLocalizedText;

        public static Action<GameObject, Action> SetMenuButtonClick;

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

        public static Func<TMP_FontAsset> GetVanillaCompassFont;

        public static Func<float> GetVanillaCompassFontSize;

        public static Action<float> ShiftHudTabBelow;

        public static Func<PlayerAction, bool, Sprite> GetActionIcon;

        public static Action<Action<BindingSourceType, InputDeviceStyle>> SubscribeInputChanged;

        public static Action<Action<BindingSourceType, InputDeviceStyle>> UnsubscribeInputChanged;
    }
}
