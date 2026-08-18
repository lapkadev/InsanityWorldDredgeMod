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
        public const string LAST_SESSION_FILE_NAME = "last-game-session.json";
    }

    public static partial class G
    {
        public static InsanityWorldConfig Config      { get; set; }
        public static LastGameSession     LastSession { get; set; }
    }

    public static partial class Funcs
    {
        public static string GetModDataDir()
        {
            string dir = Path.Combine(Application.persistentDataPath, "InsanityWorldMod");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetConfigFilePath() => Path.Combine(GetModDataDir(), CONFIG_FILE_NAME);
        public static string GetLastGameSessionFilePath() => Path.Combine(GetModDataDir(), LAST_SESSION_FILE_NAME);

        public static void LoadConfig()
        {
            var path = GetConfigFilePath();
            if (!File.Exists(path))
            {
                G.Config = new InsanityWorldConfig();
                SaveConfig();
                Log.Info($"Config: created default at {path}");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                G.Config = JsonConvert.DeserializeObject<InsanityWorldConfig>(json) ?? new InsanityWorldConfig();
                Log.Info($"Config: loaded from {path}");
            }
            catch (Exception ex)
            {
                G.Config = new InsanityWorldConfig();
                Log.Error($"Config: failed to read {path}: {ex.Message}");
            }
        }

        public static void SaveConfig()
        {
            var path = GetConfigFilePath();
            var json = JsonConvert.SerializeObject(G.Config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static void LoadLastGameSession()
        {
            var path = GetLastGameSessionFilePath();
            if (!File.Exists(path))
            {
                G.LastSession = null;
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                G.LastSession = JsonConvert.DeserializeObject<LastGameSession>(json);
                Log.Info($"LastGameSession: loaded from {path}");
            }
            catch (Exception ex)
            {
                G.LastSession = null;
                Log.Error($"LastGameSession: failed to read {path}: {ex.Message}");
            }
        }

        public static void SaveLastGameSession()
        {
            var path = GetLastGameSessionFilePath();
            var json = JsonConvert.SerializeObject(G.LastSession, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }

    public class ConfigSystem : IInsanityWorldSystem
    {
        public int Order => 1;

        public void OnLoad()
        {
            LoadConfig();
            if (G.Config.IsTransitionPhaseCompleted)
                LoadLastGameSession();
            Log.Info($"ConfigSystem: IsTransitionPhaseCompleted={G.Config.IsTransitionPhaseCompleted}, IsDev={G.Config.IsDev}");
        }
    }
}
