namespace InsanityWorldMod.Core
{
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
