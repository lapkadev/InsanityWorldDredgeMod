using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;
using static InsanityWorldMod.Core.Params;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string PFB_PORTAL_ISLAND           = "pfb_portal_island";
        public const float  PORTAL_ISLAND_SCALE         = 1f;
        public const float  PORTAL_ISLAND_WATER_LEVEL_Y = 0f;
        public const float  PORTAL_ISLAND_RADIUS_M      = 100f;

        public const float PORTAL_ISLAND_RING_INNER_M            = 3000f;
        public const float PORTAL_ISLAND_RING_OUTER_M            = 5000f;
        public const float PORTAL_ISLAND_RING_RETURN_DEG_PER_SEC = 20f;
        public const float PORTAL_ISLAND_STRAIGHT_MIN_SEC        = 10f;
        public const float PORTAL_ISLAND_STRAIGHT_MAX_SEC        = 30f;
        public const float PORTAL_ISLAND_TURN_MIN_SEC            = 5f;
        public const float PORTAL_ISLAND_TURN_MAX_SEC            = 10f;
        public const float PORTAL_ISLAND_TURN_MAX_DEG            = 60f;
        public const float PORTAL_ISLAND_JUMP_INTERVAL_SEC       = 600f;
        public const float PORTAL_ISLAND_JUMP_BLOCK_DISTANCE_M   = 150f;

        public const float PORTAL_ISLAND_JUMP_EXCLUDED_HALF_SECTOR_DEG = 5f;
        public const int   PORTAL_ISLAND_JUMP_ANGLE_ATTEMPTS           = 32;

        public static readonly Vector3 PORTAL_ISLAND_RING_CENTER = new Vector3(0f, 0f, 0f);

        public const string ISLAND_POINTER_NAME                  = "IslandPointer";
        public const string ISLAND_MARK_NAME                     = "IslandMark";
        public const int    ISLAND_POINTER_SPRITE_SIZE_PX        = 64;
        public const float  ISLAND_POINTER_SIZE_PX               = 12f;
        public const float  ISLAND_POINTER_INSET_PX              = 16f;
        public const float  ISLAND_MARK_SIZE_PX                  = 10f;
        public const float  ISLAND_POINTER_CLARITY_AT_FULL_SPEED = 0.3f;

        public static readonly Color ISLAND_POINTER_COLOR = new Color(1f, 1f, 1f, 1f);
    }

    public static partial class G
    {
        public static GameObject PortalIsland;
        public static Sprite     IslandPointerSprite;
        public static bool       IslandMotionEnabled = true;
        public static bool       IslandIsTurning;
        public static float      IslandTurnRateDegPerSec;
        public static float      IslandPhaseTimeLeft;
    }

    public static partial class Params
    {
        public static float P_PORTAL_ISLAND_SPEED = 7f;
    }

    public static partial class Funcs
    {
        public static void RefreshPortalIsland()
        {
            if (G.PortalIsland != null)
                Object.Destroy(G.PortalIsland);

            G.PortalIsland = null;

            if (!HasCompass())
            {
                Log.Info("RefreshPortalIsland: compass not granted, island not spawned");
                return;
            }

            if (!G.Save.IslandPlaced)
                JumpPortalIsland();

            var position = GetPortalIslandPosition();
            G.PortalIsland = Spawn(PFB_PORTAL_ISLAND, position, Quaternion.identity, PORTAL_ISLAND_SCALE);
            if (G.PortalIsland == null)
                return;

            MakeSolid(G.PortalIsland);
            AttachRelicParticles(G.PortalIsland.transform);
            Log.Info($"RefreshPortalIsland: spawned at {position.ToString("F1")}, jump in {G.Save.IslandJumpTimeLeft:F0}s");
        }

        public static Vector3 GetPortalIslandPosition()
        {
            return new Vector3(G.Save.IslandX, PORTAL_ISLAND_WATER_LEVEL_Y, G.Save.IslandZ);
        }

        public static void PlacePortalIsland(Vector3 position, float headingDeg)
        {
            G.Save.IslandPlaced = true;
            G.Save.IslandX = position.x;
            G.Save.IslandZ = position.z;
            G.Save.IslandHeadingDeg = Mathf.Repeat(headingDeg, 360f);
            G.Save.IslandJumpTimeLeft = PORTAL_ISLAND_JUMP_INTERVAL_SEC;

            G.IslandIsTurning = false;
            G.IslandTurnRateDegPerSec = 0f;
            G.IslandPhaseTimeLeft = Random.Range(PORTAL_ISLAND_STRAIGHT_MIN_SEC, PORTAL_ISLAND_STRAIGHT_MAX_SEC);

            if (G.PortalIsland != null)
                G.PortalIsland.transform.position = GetPortalIslandPosition();
        }

        public static void MovePortalIslandTo(Vector3 position)
        {
            if (G.Save == null)
            {
                Log.Warn("MovePortalIslandTo: save state is null");
                return;
            }

            PlacePortalIsland(position, G.Save.IslandHeadingDeg);
            Log.Info($"MovePortalIslandTo: island moved to {GetPortalIslandPosition().ToString("F1")}");
        }

        public static void SetPortalIslandMotionEnabled(bool enabled)
        {
            G.IslandMotionEnabled = enabled;
            Log.Info($"SetPortalIslandMotionEnabled: {enabled}");
        }

        public static void TickPortalIsland(float dt)
        {
            if (G.PortalIsland == null || G.Save == null || dt <= 0f)
                return;

            if (!G.IslandMotionEnabled)
                return;

            TickPortalIslandJump(dt);
            TickPortalIslandDrift(dt);
            G.PortalIsland.transform.position = GetPortalIslandPosition();
        }

        public static void TickPortalIslandJump(float dt)
        {
            G.Save.IslandJumpTimeLeft -= dt;
            if (G.Save.IslandJumpTimeLeft > 0f)
                return;

            G.Save.IslandJumpTimeLeft = PORTAL_ISLAND_JUMP_INTERVAL_SEC;

            var player = GetPlayerTransform();
            if (player != null)
            {
                float toPlayerX = player.position.x - G.Save.IslandX;
                float toPlayerZ = player.position.z - G.Save.IslandZ;
                float distance = Mathf.Sqrt(toPlayerX * toPlayerX + toPlayerZ * toPlayerZ);
                if (distance < PORTAL_ISLAND_JUMP_BLOCK_DISTANCE_M + PORTAL_ISLAND_RADIUS_M)
                {
                    Log.Info($"TickPortalIslandJump: skipped, player is {distance - PORTAL_ISLAND_RADIUS_M:F0} m from the island edge");
                    return;
                }
            }

            JumpPortalIsland();
        }

        public static void JumpPortalIsland()
        {
            if (G.Save == null)
            {
                Log.Warn("JumpPortalIsland: save state is null");
                return;
            }

            float angleDeg = PickPortalIslandJumpAngle();
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float inner = PORTAL_ISLAND_RING_INNER_M + PORTAL_ISLAND_RADIUS_M;
            float outer = PORTAL_ISLAND_RING_OUTER_M - PORTAL_ISLAND_RADIUS_M;
            float radius = Mathf.Sqrt(Random.Range(inner * inner, outer * outer));
            var position = PORTAL_ISLAND_RING_CENTER + new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad)) * radius;

            PlacePortalIsland(position, Random.Range(0f, 360f));
            Log.Info($"JumpPortalIsland: island jumped to {GetPortalIslandPosition().ToString("F1")}, angle {angleDeg:F0} deg, {radius:F0} m from center");
        }

        public static float GetRingAngleDeg(Vector3 position)
        {
            float offsetX = position.x - PORTAL_ISLAND_RING_CENTER.x;
            float offsetZ = position.z - PORTAL_ISLAND_RING_CENTER.z;
            return Mathf.Repeat(Mathf.Atan2(offsetX, offsetZ) * Mathf.Rad2Deg, 360f);
        }

        public static bool IsInsideRingSector(float angleDeg, float sectorCenterDeg)
        {
            return Mathf.Abs(Mathf.DeltaAngle(angleDeg, sectorCenterDeg)) <= PORTAL_ISLAND_JUMP_EXCLUDED_HALF_SECTOR_DEG;
        }

        public static float PickPortalIslandJumpAngle()
        {
            var player = GetPlayerTransform();
            bool hasPlayer = player != null;
            bool hasIsland = G.Save.IslandPlaced;
            float playerAngle = hasPlayer ? GetRingAngleDeg(player.position) : 0f;
            float islandAngle = hasIsland ? GetRingAngleDeg(GetPortalIslandPosition()) : 0f;

            for (int attempt = 0; attempt < PORTAL_ISLAND_JUMP_ANGLE_ATTEMPTS; attempt++)
            {
                float angle = Random.Range(0f, 360f);
                if (hasPlayer && IsInsideRingSector(angle, playerAngle))
                    continue;

                if (hasIsland && IsInsideRingSector(angle, islandAngle))
                    continue;

                return angle;
            }

            Log.Warn("PickPortalIslandJumpAngle: no free angle found, excluded sectors ignored");
            return Random.Range(0f, 360f);
        }

        public static void TickPortalIslandDrift(float dt)
        {
            G.IslandPhaseTimeLeft -= dt;
            if (G.IslandPhaseTimeLeft <= 0f)
                StartNextPortalIslandPhase();

            float offsetX = G.Save.IslandX - PORTAL_ISLAND_RING_CENTER.x;
            float offsetZ = G.Save.IslandZ - PORTAL_ISLAND_RING_CENTER.z;
            float distance = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
            float heading = G.Save.IslandHeadingDeg;
            float returnStep = PORTAL_ISLAND_RING_RETURN_DEG_PER_SEC * dt;

            if (distance + PORTAL_ISLAND_RADIUS_M > PORTAL_ISLAND_RING_OUTER_M)
                heading = Mathf.MoveTowardsAngle(heading, Mathf.Atan2(-offsetX, -offsetZ) * Mathf.Rad2Deg, returnStep);
            else if (distance - PORTAL_ISLAND_RADIUS_M < PORTAL_ISLAND_RING_INNER_M)
                heading = Mathf.MoveTowardsAngle(heading, Mathf.Atan2(offsetX, offsetZ) * Mathf.Rad2Deg, returnStep);
            else
                heading += G.IslandTurnRateDegPerSec * dt;

            float headingRad = heading * Mathf.Deg2Rad;
            G.Save.IslandHeadingDeg = Mathf.Repeat(heading, 360f);
            G.Save.IslandX += Mathf.Sin(headingRad) * P_PORTAL_ISLAND_SPEED * dt;
            G.Save.IslandZ += Mathf.Cos(headingRad) * P_PORTAL_ISLAND_SPEED * dt;
        }

        public static void StartNextPortalIslandPhase()
        {
            G.IslandIsTurning = !G.IslandIsTurning;
            if (!G.IslandIsTurning)
            {
                G.IslandTurnRateDegPerSec = 0f;
                G.IslandPhaseTimeLeft = Random.Range(PORTAL_ISLAND_STRAIGHT_MIN_SEC, PORTAL_ISLAND_STRAIGHT_MAX_SEC);
                return;
            }

            float duration = Random.Range(PORTAL_ISLAND_TURN_MIN_SEC, PORTAL_ISLAND_TURN_MAX_SEC);
            float angle = Random.Range(-PORTAL_ISLAND_TURN_MAX_DEG, PORTAL_ISLAND_TURN_MAX_DEG);
            G.IslandTurnRateDegPerSec = angle / duration;
            G.IslandPhaseTimeLeft = duration;
        }

        public static float GetIslandPointerClarity(float speed)
        {
            return Mathf.Lerp(1f, ISLAND_POINTER_CLARITY_AT_FULL_SPEED, Mathf.InverseLerp(0f, COMPASS_FULL_SPEED, speed));
        }

        public static Sprite GetIslandPointerSprite()
        {
            if (G.IslandPointerSprite != null)
                return G.IslandPointerSprite;

            int n = ISLAND_POINTER_SPRITE_SIZE_PX;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                float halfWidth = (n - y) * 0.5f;
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - n * 0.5f);
                    float edgeDist = Mathf.Min(halfWidth - dx, y);
                    float alpha = Mathf.Clamp01(edgeDist);
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false);
            G.IslandPointerSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f));
            return G.IslandPointerSprite;
        }
    }
}
