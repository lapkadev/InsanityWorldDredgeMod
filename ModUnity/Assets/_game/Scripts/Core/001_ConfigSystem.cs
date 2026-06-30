using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string CONFIG_FILE_NAME = "insanity_world_config.json";
    }

    public static partial class Funcs
    {
        public static string GetConfigFilePath()
        {
            string dir = Path.Combine(Application.persistentDataPath, "InsanityWorldMod");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, CONFIG_FILE_NAME);
        }

        public static void LoadConfig()
        {
            var path = GetConfigFilePath();
            if (!File.Exists(path))
            {
                G.Config = new InsanityWorldConfig();
                SaveConfig();
                G.Log.Info($"Config: created default at {path}");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                G.Config = JsonConvert.DeserializeObject<InsanityWorldConfig>(json) ?? new InsanityWorldConfig();
                G.Log.Info($"Config: loaded from {path}");
            }
            catch (Exception ex)
            {
                G.Config = new InsanityWorldConfig();
                G.Log.Error($"Config: failed to read {path}: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            var path = GetConfigFilePath();
            var json = JsonConvert.SerializeObject(G.Config, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }

    public class ConfigSystem : IInsanityWorldSystem
    {
        public int Order => 1;

        public void OnLoad()
        {
            LoadConfig();
            G.Log.Info($"ConfigSystem: IsTransitionPhaseCompleted={G.Config.IsTransitionPhaseCompleted}, IsDev={G.Config.IsDev}");
        }
    }
}
