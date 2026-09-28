using System;
using UnityEngine;
using InsanityWorldMod.Core;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class G
    {
        internal static TeleportState Teleport = new TeleportState();
    }

    public static partial class Funcs
    {
        public static void AddHooksTeleport()
        {
            DredgeHooks.TeleportShipToDock = (dockId, slotIndex) =>
            {
                if (!CanStartTeleport())
                    return false;

                var target = GetDockTarget(dockId, slotIndex);
                if (target == null)
                    return false;

                BeginTeleport(target.Slot.position, () => DockShipAt(target));
                return true;
            };

            DredgeHooks.TeleportShipTo = position =>
            {
                if (!CanStartTeleport())
                    return false;

                BeginTeleport(position, null);
                return true;
            };

            DredgeHooks.CancelPendingTeleport = StopTeleport;
        }

        public static bool CanStartTeleport()
        {
            if (G.Teleport.IsRunning)
            {
                Log.Debug("CanStartTeleport: already teleporting, request ignored");
                return false;
            }

            var player = G.DredgePlayer;
            if (player == null)
            {
                Log.Warn("CanStartTeleport: Player is null");
                return false;
            }

            if (player.PlayerTeleport == null)
            {
                Log.Error("CanStartTeleport: Player.PlayerTeleport is null");
                return false;
            }

            var dialogue = G.DredgeGame?.DialogueRunner;
            if (dialogue != null && dialogue.IsDialogueRunning)
            {
                Log.Warn("CanStartTeleport: dialogue is running, finish it first");
                return false;
            }

            return true;
        }

        public static void BeginTeleport(Vector3 position, Action onComplete)
        {
            G.Teleport.IsRunning = true;

            if (!G.DredgePlayer.IsDocked)
            {
                StartTeleportEffect(position, onComplete);
                return;
            }

            Log.Info("BeginTeleport: ship is docked, leaving the dock first");
            G.Teleport.OnUndocked = dock =>
            {
                if (dock != null)
                    return;

                UnsubscribeUndock();
                StartTeleportEffect(position, onComplete);
            };

            G.DredgeGameEvents.OnPlayerDockedToggled += G.Teleport.OnUndocked;
            G.DredgeGame.UI.DockUI.Leave();
        }

        public static void StartTeleportEffect(Vector3 position, Action onComplete)
        {
            G.Teleport.OnComplete = () => CompleteTeleport(onComplete);

            G.DredgeGameEvents.OnTeleportComplete += G.Teleport.OnComplete;
            G.DredgePlayer.PlayerTeleport.Teleport(position, 0f, null);
        }

        public static void CompleteTeleport(Action onComplete)
        {
            UnsubscribeTeleport();
            onComplete?.Invoke();
            G.Teleport.IsRunning = false;
        }

        public static void StopTeleport()
        {
            UnsubscribeUndock();
            UnsubscribeTeleport();

            if (G.Teleport.IsRunning)
            {
                Log.Debug("StopTeleport: clearing stuck teleport flag");
                G.Teleport.IsRunning = false;
            }
        }

        public static void UnsubscribeUndock()
        {
            if (G.Teleport.OnUndocked == null)
                return;

            if (G.DredgeGameEvents != null)
                G.DredgeGameEvents.OnPlayerDockedToggled -= G.Teleport.OnUndocked;

            G.Teleport.OnUndocked = null;
        }

        public static void UnsubscribeTeleport()
        {
            if (G.Teleport.OnComplete == null)
                return;

            if (G.DredgeGameEvents != null)
                G.DredgeGameEvents.OnTeleportComplete -= G.Teleport.OnComplete;

            G.Teleport.OnComplete = null;
        }
    }

    internal class TeleportState
    {
        public bool IsRunning;
        public Action OnComplete;
        public Action<Dock> OnUndocked;
    }
}
