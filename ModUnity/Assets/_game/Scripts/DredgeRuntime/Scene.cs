using InsanityWorldMod.Core;
using Core = InsanityWorldMod.Core;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string GAME_CANVAS_PATH = "GameCanvases/GameCanvas";
    }

    public static partial class Funcs
    {
        public static void AddListenersGameScene()
        {
            G.DredgeAppEvents.OnGameLoaded += () =>
            {
                Core.G.GameCanvas = FindUiNode(GAME_CANVAS_PATH, "game canvas");
                GameController.OnGameLoaded();
            };

            G.DredgeAppEvents.OnGameUnloaded += () => Core.G.GameCanvas = null;
        }
    }
}
