using System.Collections.Generic;
using InsanityWorldMod.Core;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public static readonly Dictionary<NotificationKind, NotificationType> NOTIFICATION_TO_DREDGE =
            new Dictionary<NotificationKind, NotificationType>
        {
            { NotificationKind.NONE,                      NotificationType.NONE },
            { NotificationKind.MONEY_GAINED,              NotificationType.MONEY_GAINED },
            { NotificationKind.MONEY_LOST,                NotificationType.MONEY_LOST },
            { NotificationKind.BOOK_ADDED,                NotificationType.BOOK_ADDED },
            { NotificationKind.BOOK_COMPLETED,            NotificationType.BOOK_COMPLETED },
            { NotificationKind.ITEM_ADDED,                NotificationType.ITEM_ADDED },
            { NotificationKind.ITEM_REMOVED,              NotificationType.ITEM_REMOVED },
            { NotificationKind.ERROR,                     NotificationType.ERROR },
            { NotificationKind.SPOOKY_EVENT,              NotificationType.SPOOKY_EVENT },
            { NotificationKind.QUEST_STARTED,             NotificationType.QUEST_STARTED },
            { NotificationKind.QUEST_UPDATED,             NotificationType.QUEST_UPDATED },
            { NotificationKind.QUEST_COMPLETED,           NotificationType.QUEST_COMPLETED },
            { NotificationKind.EQUIPMENT_DAMAGED,         NotificationType.EQUIPMENT_DAMAGED },
            { NotificationKind.EQUIPMENT_REPAIRED,        NotificationType.EQUIPMENT_REPAIRED },
            { NotificationKind.DURABILITY_LOST,           NotificationType.DURABILITY_LOST },
            { NotificationKind.CRAB_POT_DEPLOYED,         NotificationType.CRAB_POT_DEPLOYED },
            { NotificationKind.DAMAGE_TAKEN,              NotificationType.DAMAGE_TAKEN },
            { NotificationKind.ITEM_HANDED_IN,            NotificationType.ITEM_HANDED_IN },
            { NotificationKind.DEBT_REPAID,               NotificationType.DEBT_REPAID },
            { NotificationKind.ROT,                       NotificationType.ROT },
            { NotificationKind.TELEPORT_ANCHOR_PLACED,    NotificationType.TELEPORT_ANCHOR_PLACED },
            { NotificationKind.TELEPORT_ANCHOR_RETRIEVED, NotificationType.TELEPORT_ANCHOR_RETRIEVED },
            { NotificationKind.DARK_SPLASH_ADDED,         NotificationType.DARK_SPLASH_ADDED },
            { NotificationKind.ANY_REPAIR_KIT_USED,       NotificationType.ANY_REPAIR_KIT_USED },
        };

        public static readonly Dictionary<NotificationColor, DredgeColorTypeEnum> NOTIFICATION_COLOR_TO_DREDGE =
            new Dictionary<NotificationColor, DredgeColorTypeEnum>
        {
            { NotificationColor.NEUTRAL,  DredgeColorTypeEnum.NEUTRAL },
            { NotificationColor.EMPHASIS, DredgeColorTypeEnum.EMPHASIS },
            { NotificationColor.POSITIVE, DredgeColorTypeEnum.POSITIVE },
            { NotificationColor.NEGATIVE, DredgeColorTypeEnum.NEGATIVE },
            { NotificationColor.CRITICAL, DredgeColorTypeEnum.CRITICAL },
            { NotificationColor.WARNING,  DredgeColorTypeEnum.WARNING },
            { NotificationColor.VALUABLE, DredgeColorTypeEnum.VALUABLE },
            { NotificationColor.DISABLED, DredgeColorTypeEnum.DISABLED },
        };
    }

    public static partial class Funcs
    {
        public static void AddHooksNotifications()
        {
            DredgeHooks.ShowNotification = (kind, localeKey, color) =>
            {
                var ui = G.DredgeGame?.UI;
                var lang = G.DredgeGame?.LanguageManager;
                if (ui == null || lang == null)
                {
                    Log.Warn($"ShowNotification: cannot show '{localeKey}', UI or LanguageManager is null");
                    return;
                }

                ui.ShowNotificationWithColor(ToDredge(kind), localeKey, lang.GetColorCode(ToDredge(color)));
            };
        }

        public static NotificationType ToDredge(NotificationKind kind)
        {
            if (NOTIFICATION_TO_DREDGE.TryGetValue(kind, out var value))
                return value;

            Log.Warn($"Notifications: no NotificationType mapping for {kind}");
            return NotificationType.NONE;
        }

        public static DredgeColorTypeEnum ToDredge(NotificationColor color)
        {
            if (NOTIFICATION_COLOR_TO_DREDGE.TryGetValue(color, out var value))
                return value;

            Log.Warn($"Notifications: no DredgeColorTypeEnum mapping for {color}");
            return DredgeColorTypeEnum.NEUTRAL;
        }
    }
}
