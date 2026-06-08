using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace InsanityWorldMod.Editor
{
    public static partial class Constants
    {
        public const string API_ASSEMBLY_NAME  = "InsanityWorldMod.Api";
        public const string CORE_ASSEMBLY_NAME = "InsanityWorldMod.Core";
    }

    public class BuildAllArgs
    {
        public string BuildDir = "";
        public string BuildConfiguration = "Release";
    }

    public static partial class Funcs
    {
        /// <summary>
        /// Entry point for Tools/UnityRun. Parses `-args {json}` from command line.
        /// Produces DLLs + Bundles into '{args.BuildDir}/bin/{args.BuildConfiguration}/'.
        /// </summary>
        public static void BuildAll()
        {
            var parsed = ParseArgsFromCommandLine();
            if (parsed == null || string.IsNullOrEmpty(parsed.BuildDir))
            {
                Debug.LogError("[InsanityWorld] BuildAll: -args JSON missing or BuildDir not set. Aborting.");
                EditorApplication.Exit(1);
                return;
            }
            if (!BuildAll(parsed)) EditorApplication.Exit(1);
        }

        /// <summary>
        /// Produces DLLs + Bundles into '{args.BuildDir}/bin/{args.BuildConfiguration}/'.
        /// Returns true on success, false on error (caller decides exit behavior).
        /// </summary>
        public static bool BuildAll(BuildAllArgs args)
        {
            var outputDir = Path.Combine(args.BuildDir, "bin", args.BuildConfiguration);
            Directory.CreateDirectory(outputDir);
            Debug.Log($"[InsanityWorld] BuildAll: output dir = {outputDir}");

            // --- Bundles ---
            var bundlesDir = Path.Combine(outputDir, "Assets", "Bundles");
            Directory.CreateDirectory(bundlesDir);
            var manifest = BuildPipeline.BuildAssetBundles(
                bundlesDir,
                BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.StandaloneWindows64);
            int bundleCount = manifest != null ? manifest.GetAllAssetBundles().Length : 0;
            Debug.Log($"[InsanityWorld] BuildAll: built {bundleCount} bundle(s) into {bundlesDir}");

            // --- Compiled DLLs (Api, Core) ---
            var apiSrc  = $"Library/ScriptAssemblies/{Constants.API_ASSEMBLY_NAME}.dll";
            var coreSrc = $"Library/ScriptAssemblies/{Constants.CORE_ASSEMBLY_NAME}.dll";
            if (!File.Exists(apiSrc) || !File.Exists(coreSrc))
            {
                Debug.LogError($"[InsanityWorld] BuildAll: source DLLs not found:\n  {apiSrc}\n  {coreSrc}\nFix compile errors first.");
                return false;
            }
            File.Copy(apiSrc,  Path.Combine(outputDir, $"{Constants.API_ASSEMBLY_NAME}.dll"),  overwrite: true);
            File.Copy(coreSrc, Path.Combine(outputDir, $"{Constants.CORE_ASSEMBLY_NAME}.dll"), overwrite: true);
            Debug.Log($"[InsanityWorld] BuildAll: copied {Constants.API_ASSEMBLY_NAME}.dll + {Constants.CORE_ASSEMBLY_NAME}.dll into {outputDir}");

            if (args.BuildConfiguration == "Debug")
            {
                var apiPdb  = $"Library/ScriptAssemblies/{Constants.API_ASSEMBLY_NAME}.pdb";
                var corePdb = $"Library/ScriptAssemblies/{Constants.CORE_ASSEMBLY_NAME}.pdb";
                if (File.Exists(apiPdb))  File.Copy(apiPdb,  Path.Combine(outputDir, $"{Constants.API_ASSEMBLY_NAME}.pdb"),  overwrite: true);
                if (File.Exists(corePdb)) File.Copy(corePdb, Path.Combine(outputDir, $"{Constants.CORE_ASSEMBLY_NAME}.pdb"), overwrite: true);
                Debug.Log($"[InsanityWorld] BuildAll: copied .pdb files for Debug into {outputDir}");
            }

            // --- Localization JSONs ---
            var locSrc = Path.Combine(Application.dataPath, "Localization");
            if (Directory.Exists(locSrc))
            {
                var locDst = Path.Combine(outputDir, "Assets", "Localization");
                Directory.CreateDirectory(locDst);
                int locCount = 0;
                foreach (var jsonFile in Directory.GetFiles(locSrc, "*.json"))
                {
                    File.Copy(jsonFile, Path.Combine(locDst, Path.GetFileName(jsonFile)), overwrite: true);
                    locCount++;
                }
                Debug.Log($"[InsanityWorld] BuildAll: copied {locCount} localization JSON(s) into {locDst}");
            }

            // --- Dialogues (.yarn + .csv) ---
            var dialSrc = Path.Combine(Application.dataPath, "_game", "Dialogues");
            if (Directory.Exists(dialSrc))
            {
                var dialDst = Path.Combine(outputDir, "Assets", "Dialogues");
                Directory.CreateDirectory(dialDst);
                int dialCount = 0;
                foreach (var pattern in new[] { "*.yarn", "*.csv" })
                {
                    foreach (var file in Directory.GetFiles(dialSrc, pattern))
                    {
                        File.Copy(file, Path.Combine(dialDst, Path.GetFileName(file)), overwrite: true);
                        dialCount++;
                    }
                }
                Debug.Log($"[InsanityWorld] BuildAll: copied {dialCount} dialogue file(s) into {dialDst}");
            }

            Debug.Log("[InsanityWorld] BuildAll: DONE.");
            return true;
        }

        private static BuildAllArgs ParseArgsFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-args")
                {
                    var argsFile = args[i + 1];
                    try
                    {
                        var json = File.ReadAllText(argsFile);
                        return JsonConvert.DeserializeObject<BuildAllArgs>(json);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[InsanityWorld] BuildAll: failed to read/parse -args file '{argsFile}': {ex.Message}");
                        return null;
                    }
                }
            }
            return null;
        }
    }
}
