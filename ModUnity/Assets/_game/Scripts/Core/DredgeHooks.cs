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
        public static Func<IEnumerable<AssetBundle>> GetAllBundles { get; set; }

        public static Func<bool> IsPlayerSailing { get; set; }

        public static Func<DialogueRunner> GetDialogueRunner { get; set; }

        public static Action<NotificationKind, string, NotificationColor> ShowNotification { get; set; }

        public static Action RepairHull { get; set; }

        public static Action RepairAllItems { get; set; }

        public static Func<int> GetActiveSaveSlot { get; set; }

        public static Func<DockSlot?> GetLastDock { get; set; }

        public static Func<string, int, bool> MoveShipToDock { get; set; }

        public static Action CancelPendingTeleport { get; set; }

        public static Func<Transform> GetPlayerTransform { get; set; }

        public static Func<bool> IsInGame { get; set; }

        public static Action<TextMeshProUGUI> UseLocalizedFont { get; set; }

        public static Action<TextMeshProUGUI, string> UseLocalizedText { get; set; }

        public static Action<GameObject, Action> SetMenuButtonClick { get; set; }

        public static Func<Action, bool, int> AddInputBackAction { get; set; }

        public static Action<int> RemoveInputBackAction { get; set; }

        public static Action HideUnpausePrompt { get; set; }

        public static Func<string, GameObject> CreateSettingsClone { get; set; }

        public static Func<GameObject, string[], RectTransform[]> SetSettingsTabs { get; set; }

        public static Action<GameObject> ShowSettings { get; set; }

        public static Action<GameObject> HideSettings { get; set; }

        public static Action<GameObject, Action> SetSettingsCloseHandler { get; set; }

        public static Func<RectTransform> CreateMapClone { get; set; }

        public static Func<float> GetMapPixelsPerWorldUnit { get; set; }

        public static Func<TMP_FontAsset> GetVanillaCompassFont { get; set; }

        public static Func<float> GetVanillaCompassFontSize { get; set; }

        public static Action<float> ShiftHudTabBelow { get; set; }

        public static Func<PlayerAction, bool, Sprite> GetActionIcon { get; set; }

        public static Action<Action<BindingSourceType, InputDeviceStyle>> SubscribeInputChanged { get; set; }

        public static Action<Action<BindingSourceType, InputDeviceStyle>> UnsubscribeInputChanged { get; set; }
    }
}
