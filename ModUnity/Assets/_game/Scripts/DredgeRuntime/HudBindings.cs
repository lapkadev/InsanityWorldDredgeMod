using System.Reflection;
using TMPro;
using UnityEngine;
using InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string MAP_CONTENTS_FIELD   = "mapContents";
        public const string MAP_RECT_WIDTH_FIELD = "mapViewRectWidth";
        public const string MAP_CLONE_NAME       = "DredgeMapClone";
        public const string HUD_TAB_NAME         = "SlidePanelTab";
        public const float  MAP_WORLD_SIZE       = 2000f;

        public static readonly string[] MAP_CLONE_STRIPPED_LAYERS =
        {
            "OozeMarkers",
            "MapMarkers",
            "MapHarvestPOIMarkers",
            "YouAreHereMarker",
            "DemoLabels",
        };
    }

    public static partial class Funcs
    {
        public static void AddHooksHud()
        {
            DredgeHooks.GetPlayerTransform = () => G.DredgePlayer?.transform;
            DredgeHooks.GetVanillaCompassFont = () => FindDredgeCompassText()?.font;
            DredgeHooks.GetVanillaCompassFontSize = () => FindDredgeCompassText() is TextMeshProUGUI ugui ? ugui.fontSize : 0f;
            DredgeHooks.GetMapPixelsPerWorldUnit = () => ReadDredgeMapRectWidth() / MAP_WORLD_SIZE;

            DredgeHooks.CreateMapClone = () =>
            {
                var source = FindDredgeMapContents();
                if (source == null)
                    return null;

                return CloneDredgeMapContents(source);
            };

            DredgeHooks.ShiftHudTabBelow = targetTopY =>
            {
                var tab = FindDredgeHudTab();
                if (tab == null)
                    return;

                float deltaY = targetTopY - GetTopY(tab);
                if (deltaY >= 0f)
                    return;

                tab.anchoredPosition += new Vector2(0f, deltaY);
                Log.Debug($"ShiftHudTabBelow: shifted HUD tab by {deltaY}px");
            };
        }

        public static object GetPrivateField(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target);
        }

        public static RectTransform FindDredgeMapContents()
        {
            var window = FindDredgeMapWindow();
            if (window == null)
            {
                Log.Warn("FindDredgeMapContents: MapWindow not found in scene");
                return null;
            }

            var contents = GetPrivateField(window, MAP_CONTENTS_FIELD) as RectTransform;
            if (contents == null)
                Log.Warn("FindDredgeMapContents: MapWindow.mapContents not resolved");

            return contents;
        }

        public static RectTransform CloneDredgeMapContents(RectTransform source)
        {
            var clone = CloneUiNode(source.gameObject, MAP_CLONE_NAME);
            StripMapCloneLayers(clone);

            return clone.GetComponent<RectTransform>();
        }

        public static void StripMapCloneLayers(GameObject clone)
        {
            foreach (var layer in MAP_CLONE_STRIPPED_LAYERS)
            {
                var found = clone.transform.Find(layer);
                if (found != null)
                    Object.Destroy(found.gameObject);
            }
        }

        public static float ReadDredgeMapRectWidth()
        {
            var window = FindDredgeMapWindow();
            if (window == null)
                return 0f;

            var width = GetPrivateField(window, MAP_RECT_WIDTH_FIELD);
            if (width == null)
            {
                Log.Warn("ReadDredgeMapRectWidth: MapWindow.mapViewRectWidth not resolved");
                return 0f;
            }

            return (float)width;
        }

        public static RectTransform FindDredgeHudTab()
        {
            var obj = GameObject.Find(HUD_TAB_NAME);
            if (obj == null)
            {
                Log.Debug("FindDredgeHudTab: SlidePanelTab not found in scene");
                return null;
            }

            return obj.GetComponent<RectTransform>();
        }

        public static float GetTopY(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            return corners[1].y;
        }

        public static MapWindow FindDredgeMapWindow()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<MapWindow>())
            {
                if (window != null && window.gameObject.scene.IsValid())
                    return window;
            }

            return null;
        }

        public static TMP_Text FindDredgeCompassText()
        {
            var compass = Object.FindObjectOfType<CompassUI>();
            return compass == null ? null : compass.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
