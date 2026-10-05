using InsW.Core;
using UnityEngine;

namespace InsW.DredgeRuntime
{
    public static partial class G
    {
        public static GameManager DredgeGame   => GameManager.Instance;
        public static Player      DredgePlayer => GameManager.Instance?.Player;
    }

    public static partial class Funcs
    {
        public static void AddHooksPlayer()
        {
            DredgeHooks.IsInGame = () => G.DredgeGame != null && G.DredgeGame.IsPlaying && G.DredgePlayer != null;

            DredgeHooks.GetSanity = () =>
            {
                var sanity = G.DredgePlayer?.Sanity;
                if (sanity == null)
                {
                    Log.Warn("Player: sanity is null, returning full sanity");
                    return 1f;
                }

                return sanity.CurrentSanity;
            };

            DredgeHooks.ChangeSanity = delta =>
            {
                var sanity = G.DredgePlayer?.Sanity;
                if (sanity == null)
                {
                    Log.Warn("Player: sanity is null, change dropped");
                    return;
                }

                sanity.ChangeSanity(delta);
            };

            DredgeHooks.GetSanityFrameDelta = () =>
            {
                var game = G.DredgeGame;
                var player = G.DredgePlayer;
                if (game?.Time == null || player?.Sanity == null || player.SanityModifierDetector == null)
                    return 0f;

                bool ignoresTimescale = player.SanityModifierDetector.IgnoreTimescale;
                if (!ignoresTimescale && !game.Time.IsTimePassing())
                    return 0f;

                float modifier = ignoresTimescale ? 1f : game.Time.GetTimePassageModifier();
                return player.Sanity.RateOfChange * Time.deltaTime * modifier;
            };

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
