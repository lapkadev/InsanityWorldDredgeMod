using System.Collections.Generic;
using HarmonyLib;
using InsanityWorldMod.Core;
using static InsanityWorldMod.DredgeRuntime.Constants;

namespace InsanityWorldMod.DredgeRuntime
{
    public static partial class Constants
    {
        public const string ABILITY_DATAS_FIELD = "abilityDatas";
    }

    public static partial class Funcs
    {
        public static void AddHooksAbilities()
        {
            DredgeHooks.GetAbilityIds = () =>
            {
                var abilities = G.DredgeGame?.PlayerAbilities;
                if (abilities == null)
                {
                    Log.Warn("Abilities: ability manager is null");
                    return new string[0];
                }

                var datas = AccessTools.FieldRefAccess<PlayerAbilityManager, List<AbilityData>>(abilities, ABILITY_DATAS_FIELD);
                var ids = new List<string>();
                foreach (var data in datas)
                {
                    if (data == null)
                        continue;

                    ids.Add(data.name);

                    if (data.linkedAdvancedVersion != null)
                        ids.Add(data.linkedAdvancedVersion.name);
                }

                return ids.ToArray();
            };

            DredgeHooks.IsAbilityUnlocked = abilityId =>
            {
                var saveData = G.DredgeGame?.SaveData;
                if (saveData == null)
                    return false;

                return saveData.unlockedAbilities.Contains(abilityId.ToLower());
            };

            DredgeHooks.SetAbilityUnlocked = (abilityId, unlocked) =>
            {
                var game = G.DredgeGame;
                if (game?.SaveData == null || game.PlayerAbilities == null)
                {
                    Log.Warn("Abilities: game is not ready");
                    return;
                }

                string id = abilityId.ToLower();
                if (unlocked)
                {
                    if (!game.SaveData.unlockedAbilities.Contains(id))
                        game.SaveData.unlockedAbilities.Add(id);
                }
                else
                {
                    game.SaveData.unlockedAbilities.Remove(id);
                }

                G.DredgeGameEvents.TogglePlayerAbilitiesChanged(null);
            };
        }
    }
}
