using UnityEngine;
using InsW.Core;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string MENU_MODS_BUTTON_NAME = "Mods";
    }

    public static partial class Funcs
    {
        public static void AddHooksMenu()
        {
            DredgeHooks.SetMenuButtonClick = (button, onClick) =>
            {
                var wrapper = button.GetComponent<BasicButtonWrapper>();
                if (wrapper == null)
                {
                    Log.Warn("SetMenuButtonClick: button wrapper not found on cloned button");
                    return;
                }

                wrapper.OnClick = onClick;
            };

            DredgeHooks.GetModsMenuButton = () =>
            {
                var button = GameObject.Find($"{MENU_BUTTON_CONTAINER_PATH}/{MENU_MODS_BUTTON_NAME}");
                if (button == null)
                {
                    // Log.Warn("GetModsMenuButton: mods button not found in main menu");
                    return null;
                }

                return button.GetComponent<RectTransform>();
            };
        }
    }
}
