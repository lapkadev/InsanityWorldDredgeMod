using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Funcs
    {
        public static void AddHooksPlayer()
        {
            DredgeHooks.IsInGame = () => G.DredgeGame != null && G.DredgeGame.IsPlaying && G.DredgePlayer != null;

            DredgeHooks.IsPlayerSailing = () =>
            {
                var input = G.DredgeGame?.Input;
                if (G.DredgePlayer == null || input == null)
                    return false;

                return !G.DredgePlayer.IsDocked && input.GetActiveActionLayer() == ActionLayer.BASE;
            };
        }
    }
}
