using HarmonyLib;
using InsW.Core;
using UnityEngine;
using Winch.Util;
using static InsW.DredgeRuntime.Constants;

namespace InsW.DredgeRuntime
{
    public static partial class Constants
    {
        public const string BOUNDARY_WARNING_FIELD = "outOfBoundsWarning";
        public const string LEVIATHAN_ATTACK_FIELD = "attackAfterDive";
        public const string RELIC_BEAM_NODE        = "Beam";
    }

    public static partial class Funcs
    {
        public static void LogWorldEventConditions(WorldEventData data)
        {
            var game = G.DredgeGame;
            var player = G.DredgePlayer;
            if (game == null || player == null)
                return;

            var position = player.transform.position;
            float depth = game.WaveController.SampleWaterDepthAtPosition(position);
            var zone = player.PlayerZoneDetector.GetCurrentZone();
            bool safeZone = game.WorldEventManager.DoesHitSafeZone(position);

            Log.Info($"World: event '{data.name}' type {data.eventType}, prefab '{(data.prefab != null ? data.prefab.name : "none")}'");
            Log.Info($"World: player at {position.ToString("F0")}, depth {depth:F3} (event min {data.minDepth:F3}), zone {zone} (forbidden {data.forbiddenZones}), safe zone {safeZone}");

            var leviathan = data.prefab != null ? data.prefab.GetComponentInChildren<LeviathanWorldEvent>(true) : null;
            if (leviathan == null)
                return;

            bool attacks = AccessTools.FieldRefAccess<LeviathanWorldEvent, bool>(leviathan, LEVIATHAN_ATTACK_FIELD);
            Log.Info($"World: event '{data.name}' attacks after dive = {attacks}");
        }

        public static void AddHooksWorld()
        {
            DredgeHooks.MakeSolid = obj =>
            {
                if (obj == null)
                {
                    Log.Warn("World: object to make solid is null");
                    return;
                }

                var colliders = obj.GetComponentsInChildren<Collider>(true);
                foreach (var collider in colliders)
                    collider.gameObject.layer = Layer.CollidesWithPlayerAndCamera;

                Log.Info($"World: {colliders.Length} collider node(s) of '{obj.name}' moved to the solid layer");
            };

            DredgeHooks.AttachRelicParticles = parent =>
            {
                var items = G.DredgeGame?.ItemManager;
                if (items == null)
                {
                    Log.Warn("World: item manager is null");
                    return false;
                }

                RelicItemData relic = null;
                foreach (var data in items.GetAllItemsOfType<RelicItemData>())
                {
                    if (data != null && data.harvestParticlePrefab != null)
                    {
                        relic = data;
                        break;
                    }
                }

                if (relic == null)
                {
                    Log.Warn("World: no relic with a particle prefab found");
                    return false;
                }

                var obj = Object.Instantiate(relic.harvestParticlePrefab, parent);
                var particles = obj.GetComponent<HarvestableParticles>();
                if (particles == null)
                {
                    Log.Warn($"World: relic '{relic.id}' particle prefab has no HarvestableParticles");
                    Object.Destroy(obj);
                    return false;
                }

                particles.ParticlesAmount = 1;

                int disabled = 0;
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    var child = obj.transform.GetChild(i);
                    if (child.name == RELIC_BEAM_NODE)
                        continue;

                    child.gameObject.SetActive(false);
                    disabled++;
                }

                Log.Info($"World: relic '{relic.id}' beam attached to '{parent.name}', {disabled} other node(s) disabled");
                return true;
            };

            DredgeHooks.StartWorldEvent = eventId =>
            {
                var events = G.DredgeGame?.WorldEventManager;
                if (events == null)
                {
                    Log.Warn("World: world event manager is null");
                    return false;
                }

                if (events.CurrentEvent != null)
                {
                    Log.Warn($"World: event '{eventId}' not started, '{events.CurrentEvent.name}' is running");
                    return false;
                }

                var data = WorldEventUtil.GetWorldEventData(eventId);
                if (data == null)
                {
                    Log.Warn($"World: event '{eventId}' not found");
                    return false;
                }

                LogWorldEventConditions(data);
                events.DoEvent(data);
                return true;
            };

            DredgeHooks.SetBoundaryGuardEnabled = enabled =>
            {
                var guard = Object.FindObjectOfType<BoundaryEnforcer>(true);
                if (guard == null)
                {
                    Log.Warn("World: boundary guard not found");
                    return;
                }

                guard.enabled = enabled;

                var warning = AccessTools.FieldRefAccess<BoundaryEnforcer, GameObject>(guard, BOUNDARY_WARNING_FIELD);
                if (!enabled && warning != null)
                    warning.SetActive(false);

                Log.Info($"World: boundary guard enabled set to {enabled}");
            };
        }
    }
}
