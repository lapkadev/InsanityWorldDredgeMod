using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string YARN_FN_GET_CHARGE                 = "lapkadev_get_charge";
        public const string YARN_FN_GET_MAX_CHARGE             = "lapkadev_get_max_charge";
        public const string YARN_FN_IS_CELL_FULL               = "lapkadev_is_cell_full";

        public const string YARN_CMD_ADD_CHARGE                = "lapkadev_add_charge";
        public const string YARN_CMD_ADD_CHARGE_PER_ABERRATION = "lapkadev_add_charge_per_aberration";
    }

    public static partial class Funcs
    {
        public static int GetInsanityCharge()
        {
            return G.Save?.InsanityCellCharge ?? 0;
        }

        public static int GetInsanityMaxCharge()
        {
            return INSANITY_CELL_MAX_CHARGE;
        }

        public static bool IsInsanityCellFull()
        {
            return GetInsanityCharge() >= INSANITY_CELL_MAX_CHARGE;
        }

        public static void AddInsanityCharge(int count)
        {
            if (G.Save == null)
            {
                Log.Warn($"AddInsanityCharge: save state is null, +{count} dropped");
                return;
            }

            int newCharge = G.Save.InsanityCellCharge + count;
            if (newCharge > INSANITY_CELL_MAX_CHARGE)
                newCharge = INSANITY_CELL_MAX_CHARGE;

            G.Save.InsanityCellCharge = newCharge;
            Log.Info($"AddInsanityCharge: +{count} -> {newCharge}/{INSANITY_CELL_MAX_CHARGE}");
            Save();
        }

        public static void AddInsanityChargePerAberration(int chargePerItem)
        {
            int count = GetLastGridAberrationCount();
            int total = count * chargePerItem;

            Log.Info($"AddInsanityChargePerAberration: {count} aberrations x {chargePerItem} = +{total}");
            AddInsanityCharge(total);
        }
    }
}
