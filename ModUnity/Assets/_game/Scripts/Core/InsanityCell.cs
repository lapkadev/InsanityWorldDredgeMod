namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const int    INSANITY_CELL_MAX_CHARGE       = 100;
        public const int    INSANITY_CHARGE_PER_ABERRATION = 10;

        public const string YARN_FN_GET_CHARGE                 = "insanity_get_charge";
        public const string YARN_FN_GET_MAX_CHARGE             = "insanity_get_max_charge";
        public const string YARN_FN_IS_CELL_FULL               = "insanity_is_cell_full";
        public const string YARN_FN_IS_CELL_CRAFTED            = "insanity_is_cell_crafted";

        public const string YARN_CMD_CRAFT_CELL                = "insanity_craft_cell";
        public const string YARN_CMD_ADD_CHARGE                = "insanity_add_charge";
        public const string YARN_CMD_ADD_CHARGE_PER_ABERRATION = "insanity_add_charge_per_aberration";
    }

    // public static partial class Funcs
    // {
    //     public static int GetInsanityCharge()
    //     {
    //         return G.Save?.InsanityCellCharge ?? 0;
    //     }
    //
    //     public static int GetInsanityMaxCharge()
    //     {
    //         return INSANITY_CELL_MAX_CHARGE;
    //     }
    //
    //     public static bool IsInsanityCellFull()
    //     {
    //         return GetInsanityCharge() >= INSANITY_CELL_MAX_CHARGE;
    //     }
    //
    //     public static bool IsInsanityCellCrafted()
    //     {
    //         return G.Save != null && G.Save.InsanityCellCrafted;
    //     }
    //
    //     public static void CraftInsanityCell()
    //     {
    //         if (G.Save == null)
    //         {
    //             Log.Warn("CraftInsanityCell: save state is null, craft dropped");
    //             return;
    //         }
    //
    //         G.Save.InsanityCellCrafted = true;
    //         Log.Info("CraftInsanityCell: cell marked as crafted");
    //         Save();
    //     }
    //
    //     public static void AddInsanityCharge(int count)
    //     {
    //         if (G.Save == null)
    //         {
    //             Log.Warn($"AddInsanityCharge: save state is null, +{count} dropped");
    //             return;
    //         }
    //
    //         int newCharge = G.Save.InsanityCellCharge + count;
    //         if (newCharge > INSANITY_CELL_MAX_CHARGE)
    //             newCharge = INSANITY_CELL_MAX_CHARGE;
    //
    //         G.Save.InsanityCellCharge = newCharge;
    //         Log.Info($"AddInsanityCharge: +{count} -> {newCharge}/{INSANITY_CELL_MAX_CHARGE}");
    //         Save();
    //     }
    //
    //     public static void AddInsanityChargePerAberration(int chargePerItem)
    //     {
    //         int count = GetLastGridAberrationCount();
    //         int total = count * chargePerItem;
    //
    //         Log.Info($"AddInsanityChargePerAberration: {count} aberrations x {chargePerItem} = +{total}");
    //         AddInsanityCharge(total);
    //     }
    // }
}
