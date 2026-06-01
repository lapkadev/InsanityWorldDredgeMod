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
}
