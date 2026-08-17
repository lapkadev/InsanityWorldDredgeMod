using InControl;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class G
    {
        public static KeyBindings Bindings { get; set; }
    }

    public class KeyBindings : PlayerActionSet
    {
        public readonly PlayerAction ToggleCompass;

        public KeyBindings()
        {
            ToggleCompass = CreatePlayerAction("insanity.binding.toggle-compass");
            ToggleCompass.AddDefaultBinding(Key.C);
            ToggleCompass.AddDefaultBinding(InputControlType.LeftStickButton);
        }
    }

    public static partial class Funcs
    {
        public static void InitKeyBindings()
        {
            if (G.Bindings != null) return;

            G.Bindings = new KeyBindings();

            var saved = G.Config?.KeyBindings;
            if (!string.IsNullOrEmpty(saved))
            {
                G.Bindings.Load(saved);
                G.Log.Info("KeyBindings: loaded from config");
            }
            else
            {
                G.Log.Info("KeyBindings: using defaults");
            }
        }

        public static void SaveKeyBindings()
        {
            if (G.Bindings == null || G.Config == null) return;

            G.Config.KeyBindings = G.Bindings.Save();
            SaveConfig();
            G.Log.Info("KeyBindings: saved to config");
        }
    }
}
