using InsW.Core;
using static InsW.Core.Funcs;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string GAME_CANVAS_PATH = "GameCanvases/GameCanvas";
    }

    public static partial class G
    {
        public static ApplicationEvents DredgeAppEvents => ApplicationEvents.Instance;
    }

    public static partial class Funcs
    {
        public static void AddEventsGameScene()
        {
            G.DredgeAppEvents.OnGameLoaded += () =>
            {
                Core.G.GameCanvas = FindUiNode(GAME_CANVAS_PATH, "game canvas");
                DredgeEvents.OnGameLoaded?.Invoke();
            };

            G.DredgeAppEvents.OnGameUnloaded += () => Core.G.GameCanvas = null;
        }
    }
}
