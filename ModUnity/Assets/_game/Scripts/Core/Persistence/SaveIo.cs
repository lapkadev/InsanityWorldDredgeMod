using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.DredgeHooks;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const bool USE_DEBUG_PATH = false;
    }

    public static partial class Funcs
    {
        public static void Save()
        {
            if (G.Game == null || G.Save == null)
            {
                Log.Warn("Save: state not initialized");
                return;
            }

            G.Game.CaptureFromDredge();

            var slot = ResolveSlot("last");
            if (slot < 0)
            {
                Log.Debug("Save: no active slot yet, skipping");
                return;
            }

            try
            {
                var path = GetSaveFilePath(slot);
                var json = JsonConvert.SerializeObject(G.Save, Formatting.Indented);
                File.WriteAllText(path, json);
                Log.Debug($"Save: slot={slot} -> {path}");
            }
            catch (Exception ex)
            {
                Log.Error($"Save: failed to write slot {slot}: {ex}");
            }
        }

        public static void Load(string save = "last")
        {
            var slot = ResolveSlot(save);
            if (slot < 0)
            {
                Log.Warn($"Load: cannot resolve slot from '{save}'");
                return;
            }

            JToken token = null;
            var path = GetSaveFilePath(slot);
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    token = JToken.Parse(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"Load: failed to parse {path}, using default: {ex}");
                }
            }

            G.Save = token == null
                ? new SaveState()
                : SaveStateMigrator.MigrateAndDeserialize(token);

            G.Game = new GameState();
            G.Game.InitFromSave();
            G.Game.ApplyToDredge();

            Log.Info($"Load: slot={slot}, TotalRuns={G.Save.TotalRuns}, TotalDeathsIntercepted={G.Save.TotalDeathsIntercepted}");
        }

        public static string GetSaveFilePath(int slot)
        {
            string dir = USE_DEBUG_PATH
                ? Path.Combine(G.ModBasePath, "saves")
                : Path.Combine(Application.persistentDataPath, "InsanityWorldMod", "saves");

            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"slot_{slot}.json");
        }

        public static int ResolveSlot(string save)
        {
            if (save == "last")
                return GetActiveSaveSlot();

            return int.TryParse(save, out var n) ? n : -1;
        }
    }
}
