using UnityEngine;

namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Screen corner anchoring for HUD widgets (compass, future minimap, etc.).
    /// </summary>
    public enum HudCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    public static partial class Constants
    {
        public const HudCorner MINIMAP_CORNER = HudCorner.TopRight;
    }

    public static partial class Funcs
    {
        public static void AnchorToCorner(RectTransform rt, HudCorner corner, float margin)
        {
            switch (corner)
            {
                case HudCorner.TopLeft:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(margin, -margin);
                    break;
                case HudCorner.TopRight:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-margin, -margin);
                    break;
                case HudCorner.BottomLeft:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(margin, margin);
                    break;
                case HudCorner.BottomRight:
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-margin, margin);
                    break;
            }
        }
    }
}
