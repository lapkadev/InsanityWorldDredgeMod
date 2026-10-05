using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace InsW.Core
{
    public static class WinchHooks
    {
        /// <summary>
        /// Returns every AssetBundle Winch has loaded.
        /// </summary>
        public static Func<IEnumerable<AssetBundle>> GetAllBundles;

        public static Func<List<InstalledModInfo>> GetInstalledMods;

        public static Func<string, RectTransform> GetModsTabEntry;

        public static Func<TMP_Text> GetModsTabTitle;

        public static Func<RectTransform> GetModsTabListArea;

        public static Action<bool> SetModsTabShortcutsEnabled;

        public static Func<string, bool> IsModEnabled;

        public static Action<string, bool> SetModEnabled;

        public static Func<string> GetModsDir;
    }

    public class InstalledModInfo
    {
        public string Guid;
        public string Name;
        public string Version;
        public string Dir;
        public bool IsLoaded;
    }
}
