using InControl;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string ACTION_TOGGLE_COMPASS = "toggle-compass";
    }

    public static partial class G
    {
        public static KeyBindings Bindings { get; set; }
    }

    public static partial class Funcs
    {
        public static void InitKeyBindings()
        {
            if (G.Bindings != null)
                return;

            G.Bindings = new KeyBindings();
            Log.Info("KeyBindings: actions created");
        }
    }

    public class KeyBindings : PlayerActionSet
    {
        public readonly PlayerAction ToggleCompass;

        public KeyBindings()
        {
            ToggleCompass = CreatePlayerAction(ACTION_TOGGLE_COMPASS);
            ToggleCompass.AddDefaultBinding(Key.C);
            ToggleCompass.AddDefaultBinding(InputControlType.LeftStickButton);
        }
    }
}
