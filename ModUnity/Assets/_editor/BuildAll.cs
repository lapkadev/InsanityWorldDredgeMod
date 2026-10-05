using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
using static InsW.Editor.Constants;

namespace InsW.Editor
{
    public static partial class Constants
    {
        public const string API_ASSEMBLY_NAME       = "InsW.DredgeRuntime";
        public const string CORE_ASSEMBLY_NAME      = "InsW.Core";
        public const string PLAYER_SCRIPTS_DIR      = "Library/InsanityWorldPlayerScripts";
        public const string EDITOR_SCRIPTS_DIR      = "Library/ScriptAssemblies";
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
        /// Produces DLLs into '{args.BuildDir}/bin/{args.BuildConfiguration}/'.
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
        /// Produces DLLs into '{args.BuildDir}/bin/{args.BuildConfiguration}/'.
        /// Returns true on success, false on error (caller decides exit behavior).
        /// </summary>
        public static bool BuildAll(BuildAllArgs args)
        {
            var outputDir = Path.Combine(args.BuildDir, "bin", args.BuildConfiguration);
            Directory.CreateDirectory(outputDir);
            Debug.Log($"[InsanityWorld] BuildAll: output dir = {outputDir}");

            var playerDir = CompilePlayerAssemblies(args.BuildConfiguration == "Release");
            if (playerDir == null)
                return false;

            // --- Compiled DLLs (Api, Core) ---
            var apiSrc  = $"{EDITOR_SCRIPTS_DIR}/{API_ASSEMBLY_NAME}.dll";
            var coreSrc = $"{playerDir}/{CORE_ASSEMBLY_NAME}.dll";
            if (!File.Exists(apiSrc) || !File.Exists(coreSrc))
            {
                Debug.LogError($"[InsanityWorld] BuildAll: source DLLs not found:\n  {apiSrc}\n  {coreSrc}\nFix compile errors first.");
                return false;
            }
            File.Copy(apiSrc,  Path.Combine(outputDir, $"{API_ASSEMBLY_NAME}.dll"),  overwrite: true);
            File.Copy(coreSrc, Path.Combine(outputDir, $"{CORE_ASSEMBLY_NAME}.dll"), overwrite: true);
            Debug.Log($"[InsanityWorld] BuildAll: copied {API_ASSEMBLY_NAME}.dll + {CORE_ASSEMBLY_NAME}.dll into {outputDir}");

            if (args.BuildConfiguration == "Debug")
            {
                var apiPdb  = $"{EDITOR_SCRIPTS_DIR}/{API_ASSEMBLY_NAME}.pdb";
                var corePdb = $"{playerDir}/{CORE_ASSEMBLY_NAME}.pdb";
                if (File.Exists(apiPdb))  File.Copy(apiPdb,  Path.Combine(outputDir, $"{API_ASSEMBLY_NAME}.pdb"),  overwrite: true);
                if (File.Exists(corePdb)) File.Copy(corePdb, Path.Combine(outputDir, $"{CORE_ASSEMBLY_NAME}.pdb"), overwrite: true);
                Debug.Log($"[InsanityWorld] BuildAll: copied .pdb files for Debug into {outputDir}");
            }

            Debug.Log("[InsanityWorld] BuildAll: DONE.");
            return true;
        }

        public static string CompilePlayerAssemblies(bool release)
        {
            var playerDir = Path.GetFullPath(PLAYER_SCRIPTS_DIR);
            if (Directory.Exists(playerDir))
                Directory.Delete(playerDir, recursive: true);

            Directory.CreateDirectory(playerDir);

            var settings = new ScriptCompilationSettings
            {
                group   = BuildTargetGroup.Standalone,
                target  = BuildTarget.StandaloneWindows,
                options = release ? ScriptCompilationOptions.None : ScriptCompilationOptions.DevelopmentBuild,
            };

            var result = PlayerBuildInterface.CompilePlayerScripts(settings, playerDir);
            if (result.assemblies == null || result.assemblies.Count == 0)
            {
                Debug.LogError($"[InsanityWorld] CompilePlayerAssemblies: player script compilation produced no assemblies in {playerDir}");
                return null;
            }

            var found = Directory.GetFiles(playerDir, $"{CORE_ASSEMBLY_NAME}.dll", SearchOption.AllDirectories);
            if (found.Length == 0)
            {
                Debug.LogError($"[InsanityWorld] CompilePlayerAssemblies: {CORE_ASSEMBLY_NAME}.dll not found under {playerDir}. Reported assemblies: {string.Join(", ", result.assemblies)}");
                return null;
            }

            var assembliesDir = Path.GetDirectoryName(found[0]);
            Debug.Log($"[InsanityWorld] CompilePlayerAssemblies: compiled {result.assemblies.Count} player assembly(ies) into {assembliesDir}");
            return assembliesDir;
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
