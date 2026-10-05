using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using static InsW.Core.Constants;

namespace InsW.Core
{
    public static partial class Constants
    {
        public const string MOD_FILES_FILE = "mod_files.json";
    }

    public static partial class G
    {
        public static string       ModDir;
        public static ModFilesList ModFiles;
    }

    public static partial class Funcs
    {
        public static void LoadModFilesJson()
        {
            try
            {
                var str = File.ReadAllText(Path.Combine(G.ModDir, MOD_FILES_FILE));
                G.ModFiles = JsonConvert.DeserializeObject<ModFilesList>(str);
                if (G.ModFiles == null)
                    Log.Error($"ModFiles: {MOD_FILES_FILE} is empty");
            }
            catch (Exception ex)
            {
                Log.Error($"ModFiles: failed to read {MOD_FILES_FILE}: {ex.Message}");
            }
        }

        public static void LoadModAssemblies()
        {
            if (G.ModFiles?.Assemblies == null)
                return;

            foreach (var name in G.ModFiles.Assemblies)
            {
                try
                {
                    Assembly.LoadFrom(Path.Combine(G.ModDir, name));
                }
                catch (Exception ex)
                {
                    Log.Error($"ModFiles: failed to load {name}: {ex.Message}");
                }
            }
        }
    }

    public class ModFilesList
    {
        public List<string> Assemblies = new List<string>();
        public List<string> Files      = new List<string>();
    }
}
