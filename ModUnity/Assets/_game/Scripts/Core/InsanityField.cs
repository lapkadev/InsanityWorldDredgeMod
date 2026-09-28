using System.Collections.Generic;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const float FIELD_CELL_M           = 50f;
        public const int   FIELD_WINDOW_CELLS     = 60;
        public const int   FIELD_MAX_FUEL         = 4;
        public const float FIELD_INNER_RADIUS_M   = 1800f;
        public const float FIELD_STEP_SEC         = 1f;
        public const float FIELD_GROWTH_CHANCE    = 0.002f;
        public const float FIELD_LIGHTNING_CHANCE = 0.000002f;
        public const float FIELD_INIT_DENSITY     = 0.35f;
        public const float FIELD_INSANITY_PER_SEC = 1f / 60f;
        public const float FIELD_BANISH_RANGE_M   = 600f;

        public const float FIELD_HEX_ROW_M = 0.8660254f;

        public const string FIELD_LAYER_NAME                  = "InsanityField";
        public const int    FIELD_LAYER_TEXTURE_PX            = 64;
        public const float  FIELD_LAYER_MARGIN                = 1.5f;
        public const float  FIELD_LAYER_RECENTER_SHARE        = 0.15f;
        public const float  FIELD_LAYER_REFRESH_SEC           = 0.2f;
        public const float  FIELD_LAYER_MAX_ALPHA             = 0.55f;
        public const float  FIELD_LAYER_CLARITY_AT_FULL_SPEED = 0.3f;

        public static readonly Color FIELD_LAYER_COLOR = new Color(0.62f, 0.08f, 0.42f, 1f);

        public static readonly Vector3 FIELD_CENTER = new Vector3(0f, 0f, 0f);

        public static readonly Vector2Int[] FIELD_HEX_NEIGHBORS =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, -1), new Vector2Int(-1, 1),
        };
    }

    public static partial class G
    {
        public static InsanityFieldState Field = new InsanityFieldState();
    }

    public static partial class Funcs
    {
        public static void ResetInsanityField()
        {
            G.Field.Cells = null;
            G.Field.StepTimeLeft = FIELD_STEP_SEC;
            G.Field.Seed = Random.Range(1, int.MaxValue);
            Log.Info($"ResetInsanityField: seed {G.Field.Seed}");
        }

        public static bool IsInsanityFieldActive()
        {
            return HasCompass() && G.PortalIsland != null && G.Game != null && G.Game.IsLoaded && G.IsInGame;
        }

        public static void TickInsanityField(float dt)
        {
            if (dt <= 0f || !IsInsanityFieldActive())
                return;

            var player = GetPlayerTransform();
            if (player == null)
                return;

            MoveInsanityFieldWindow(player.position);

            G.Field.StepTimeLeft -= dt;
            while (G.Field.StepTimeLeft <= 0f)
            {
                G.Field.StepTimeLeft += FIELD_STEP_SEC;
                StepInsanityField();
            }

            float value = GetInsanityFieldAt(player.position);
            if (value > 0f)
                G.Game.InsanityLevel += value * FIELD_INSANITY_PER_SEC * dt;
        }

        public static Vector2Int WorldToHex(Vector3 position)
        {
            float x = (position.x - FIELD_CENTER.x) / FIELD_CELL_M;
            float z = (position.z - FIELD_CENTER.z) / FIELD_CELL_M;
            float row = z / FIELD_HEX_ROW_M;
            float col = x - row * 0.5f;
            return RoundHex(col, row);
        }

        public static Vector3 HexToWorld(int col, int row)
        {
            float x = (col + row * 0.5f) * FIELD_CELL_M + FIELD_CENTER.x;
            float z = row * FIELD_HEX_ROW_M * FIELD_CELL_M + FIELD_CENTER.z;
            return new Vector3(x, 0f, z);
        }

        public static Vector2Int RoundHex(float col, float row)
        {
            float third = -col - row;
            int roundedCol = Mathf.RoundToInt(col);
            int roundedRow = Mathf.RoundToInt(row);
            int roundedThird = Mathf.RoundToInt(third);

            float colDiff = Mathf.Abs(roundedCol - col);
            float rowDiff = Mathf.Abs(roundedRow - row);
            float thirdDiff = Mathf.Abs(roundedThird - third);

            if (colDiff > rowDiff && colDiff > thirdDiff)
                roundedCol = -roundedRow - roundedThird;
            else if (rowDiff > thirdDiff)
                roundedRow = -roundedCol - roundedThird;

            return new Vector2Int(roundedCol, roundedRow);
        }

        public static int FieldIndex(int localCol, int localRow)
        {
            return localRow * FIELD_WINDOW_CELLS + localCol;
        }

        public static bool IsInsideFieldWindow(int localCol, int localRow)
        {
            return localCol >= 0 && localCol < FIELD_WINDOW_CELLS && localRow >= 0 && localRow < FIELD_WINDOW_CELLS;
        }

        public static void MoveInsanityFieldWindow(Vector3 position)
        {
            var field = G.Field;
            var center = WorldToHex(position);
            int originCol = center.x - FIELD_WINDOW_CELLS / 2;
            int originRow = center.y - FIELD_WINDOW_CELLS / 2;
            if (field.Cells != null && originCol == field.OriginCol && originRow == field.OriginRow)
                return;

            var cells = new byte[FIELD_WINDOW_CELLS * FIELD_WINDOW_CELLS];
            for (int row = 0; row < FIELD_WINDOW_CELLS; row++)
            {
                for (int col = 0; col < FIELD_WINDOW_CELLS; col++)
                {
                    int cellCol = originCol + col;
                    int cellRow = originRow + row;
                    int oldCol = cellCol - field.OriginCol;
                    int oldRow = cellRow - field.OriginRow;
                    bool inOld = field.Cells != null && IsInsideFieldWindow(oldCol, oldRow);

                    cells[FieldIndex(col, row)] = inOld
                        ? field.Cells[FieldIndex(oldCol, oldRow)]
                        : InitInsanityFieldCell(cellCol, cellRow);
                }
            }

            field.Cells = cells;
            field.OriginCol = originCol;
            field.OriginRow = originRow;
        }

        public static byte InitInsanityFieldCell(int cellCol, int cellRow)
        {
            if (IsInsideInsanityFieldHole(cellCol, cellRow))
                return 0;

            uint hash = HashInsanityFieldCell(cellCol, cellRow, G.Field.Seed);
            float roll = (hash & 0xFFFF) / 65535f;
            if (roll >= FIELD_INIT_DENSITY)
                return 0;

            return (byte)(1 + (int)((hash >> 16) % FIELD_MAX_FUEL));
        }

        public static uint HashInsanityFieldCell(int cellCol, int cellRow, int seed)
        {
            uint hash = (uint)seed;
            hash ^= (uint)cellCol * 0x9E3779B1u;
            hash = (hash ^ (hash >> 15)) * 0x85EBCA77u;
            hash ^= (uint)cellRow * 0xC2B2AE3Du;
            hash = (hash ^ (hash >> 13)) * 0x27D4EB2Fu;
            return hash ^ (hash >> 16);
        }

        public static bool IsInsideInsanityFieldHole(int cellCol, int cellRow)
        {
            var center = HexToWorld(cellCol, cellRow) - FIELD_CENTER;
            return center.x * center.x + center.z * center.z < FIELD_INNER_RADIUS_M * FIELD_INNER_RADIUS_M;
        }

        public static void StepInsanityField()
        {
            var field = G.Field;
            for (int row = 0; row < FIELD_WINDOW_CELLS; row++)
            {
                for (int col = 0; col < FIELD_WINDOW_CELLS; col++)
                {
                    int index = FieldIndex(col, row);
                    byte fuel = field.Cells[index];
                    if (fuel == 0 && IsInsideInsanityFieldHole(field.OriginCol + col, field.OriginRow + row))
                        continue;

                    if (fuel < FIELD_MAX_FUEL && Random.value < FIELD_GROWTH_CHANCE)
                        field.Cells[index] = (byte)(fuel + 1);
                    else if (fuel > 0 && Random.value < FIELD_LIGHTNING_CHANCE)
                        BurnInsanityField(col, row, 0f);
                }
            }
        }

        public static int BurnInsanityField(int startCol, int startRow, float rangeM)
        {
            var field = G.Field;
            if (field.Cells == null || field.Cells[FieldIndex(startCol, startRow)] == 0)
                return 0;

            var origin = HexToWorld(field.OriginCol + startCol, field.OriginRow + startRow);
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startCol, startRow));
            field.Cells[FieldIndex(startCol, startRow)] = 0;
            int burned = 1;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in FIELD_HEX_NEIGHBORS)
                {
                    int nextCol = cell.x + step.x;
                    int nextRow = cell.y + step.y;
                    if (!IsInsideFieldWindow(nextCol, nextRow))
                        continue;

                    int index = FieldIndex(nextCol, nextRow);
                    if (field.Cells[index] == 0)
                        continue;

                    if (rangeM > 0f)
                    {
                        float distance = Vector3.Distance(HexToWorld(field.OriginCol + nextCol, field.OriginRow + nextRow), origin);
                        if (Random.value >= 1f - distance / rangeM)
                            continue;
                    }

                    field.Cells[index] = 0;
                    burned++;
                    queue.Enqueue(new Vector2Int(nextCol, nextRow));
                }
            }

            return burned;
        }

        public static void StrikeInsanityField(Vector3 position)
        {
            if (G.Field.Cells == null)
            {
                Log.Warn("StrikeInsanityField: field is not active");
                return;
            }

            var hex = WorldToHex(position);
            int col = hex.x - G.Field.OriginCol;
            int row = hex.y - G.Field.OriginRow;
            if (!IsInsideFieldWindow(col, row))
            {
                Log.Warn($"StrikeInsanityField: {position.ToString("F0")} is outside the field window");
                return;
            }

            int burned = BurnInsanityField(col, row, FIELD_BANISH_RANGE_M);
            Log.Info($"StrikeInsanityField: {burned} cell(s) cleared around {position.ToString("F0")}");
        }

        public static float GetInsanityFieldAt(Vector3 position)
        {
            var field = G.Field;
            if (field.Cells == null)
                return 0f;

            var hex = WorldToHex(position);
            int col = hex.x - field.OriginCol;
            int row = hex.y - field.OriginRow;

            float weightSum = 0f;
            float valueSum = 0f;
            AccumulateInsanityFieldSample(position, col, row, ref weightSum, ref valueSum);
            foreach (var step in FIELD_HEX_NEIGHBORS)
                AccumulateInsanityFieldSample(position, col + step.x, row + step.y, ref weightSum, ref valueSum);

            return weightSum > 0f ? valueSum / weightSum : 0f;
        }

        public static float GetInsanityFieldLayerClarity(float speed)
        {
            return Mathf.Lerp(1f, FIELD_LAYER_CLARITY_AT_FULL_SPEED, Mathf.InverseLerp(0f, COMPASS_FULL_SPEED, speed));
        }

        public static void FillInsanityFieldTexture(Color32[] pixels, int size, Vector3 center, float sideM)
        {
            var color = FIELD_LAYER_COLOR;
            for (int j = 0; j < size; j++)
            {
                float z = center.z + ((j + 0.5f) / size - 0.5f) * sideM;
                for (int i = 0; i < size; i++)
                {
                    float x = center.x + ((i + 0.5f) / size - 0.5f) * sideM;
                    color.a = GetInsanityFieldAt(new Vector3(x, 0f, z)) * FIELD_LAYER_MAX_ALPHA;
                    pixels[j * size + i] = color;
                }
            }
        }

        public static void AccumulateInsanityFieldSample(Vector3 position, int col, int row, ref float weightSum, ref float valueSum)
        {
            if (!IsInsideFieldWindow(col, row))
                return;

            var center = HexToWorld(G.Field.OriginCol + col, G.Field.OriginRow + row);
            float dx = position.x - center.x;
            float dz = position.z - center.z;
            float weight = Mathf.Max(0f, 1f - Mathf.Sqrt(dx * dx + dz * dz) / FIELD_CELL_M);
            if (weight <= 0f)
                return;

            weightSum += weight;
            valueSum += weight * G.Field.Cells[FieldIndex(col, row)] / FIELD_MAX_FUEL;
        }
    }

    public class InsanityFieldState
    {
        public byte[] Cells;
        public int OriginCol;
        public int OriginRow;
        public int Seed;
        public float StepTimeLeft;
    }
}
