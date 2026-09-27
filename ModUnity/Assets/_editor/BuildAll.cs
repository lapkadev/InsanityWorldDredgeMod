using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using static InsanityWorldMod.Editor.Constants;

namespace InsanityWorldMod.Editor
{
    public static partial class Constants
    {
        public const string API_ASSEMBLY_NAME       = "InsanityWorldMod.DredgeRuntime";
        public const string CORE_ASSEMBLY_NAME      = "InsanityWorldMod.Core";
        public const string TRANSLATIONS_DIR_NAME   = "tr";

        public static readonly string[] MIRROR_RUNTIME_ASSEMBLIES =
        {
            "Mirror",
            "Mirror.Components",
            "Mirror.Transports",
            "Mirror.Authenticators",
            "Telepathy",
            "kcp2k",
            "SimpleWebTransport",
        };
    }

    public class BuildAllArgs
    {
        public string BuildDir = "";
        public string BuildConfiguration = "Release";
    }

    public static partial class Funcs
    {
        /// <summary>
        /// Deletes '{args.BuildDir}/bin/{args.BuildConfiguration}/'.
        /// Returns true on success, false on error (caller decides exit behavior).
        /// </summary>
        public static bool CleanBuildDir(BuildAllArgs args)
        {
            try
            {
                var outputDir = Path.Combine(args.BuildDir, "bin", args.BuildConfiguration);
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, recursive: true);
                    Debug.Log($"[InsanityWorld] CleanBuildDir: cleaned {outputDir}");
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InsanityWorld] CleanBuildDir: {ex.Message}");
                return false;
            }
        }

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
            var apiSrc  = $"Library/ScriptAssemblies/{API_ASSEMBLY_NAME}.dll";
            var coreSrc = $"Library/ScriptAssemblies/{CORE_ASSEMBLY_NAME}.dll";
            if (!File.Exists(apiSrc) || !File.Exists(coreSrc))
            {
                Debug.LogError($"[InsanityWorld] BuildAll: source DLLs not found:\n  {apiSrc}\n  {coreSrc}\nFix compile errors first.");
                return false;
            }
            File.Copy(apiSrc,  Path.Combine(outputDir, $"{API_ASSEMBLY_NAME}.dll"),  overwrite: true);
            File.Copy(coreSrc, Path.Combine(outputDir, $"{CORE_ASSEMBLY_NAME}.dll"), overwrite: true);
            Debug.Log($"[InsanityWorld] BuildAll: copied {API_ASSEMBLY_NAME}.dll + {CORE_ASSEMBLY_NAME}.dll into {outputDir}");

            // --- Mirror runtime DLLs ---
            foreach (var asm in MIRROR_RUNTIME_ASSEMBLIES)
            {
                var mirrorSrc = $"Library/ScriptAssemblies/{asm}.dll";
                if (!File.Exists(mirrorSrc))
                {
                    Debug.LogError($"[InsanityWorld] BuildAll: Mirror runtime assembly not found: {mirrorSrc}\nDid bootstrap install Mirror and did it compile?");
                    return false;
                }
                File.Copy(mirrorSrc, Path.Combine(outputDir, $"{asm}.dll"), overwrite: true);
            }
            Debug.Log($"[InsanityWorld] BuildAll: copied {MIRROR_RUNTIME_ASSEMBLIES.Length} Mirror runtime DLL(s) into {outputDir}");

            if (args.BuildConfiguration == "Debug")
            {
                var apiPdb  = $"Library/ScriptAssemblies/{API_ASSEMBLY_NAME}.pdb";
                var corePdb = $"Library/ScriptAssemblies/{CORE_ASSEMBLY_NAME}.pdb";
                if (File.Exists(apiPdb))  File.Copy(apiPdb,  Path.Combine(outputDir, $"{API_ASSEMBLY_NAME}.pdb"),  overwrite: true);
                if (File.Exists(corePdb)) File.Copy(corePdb, Path.Combine(outputDir, $"{CORE_ASSEMBLY_NAME}.pdb"), overwrite: true);
                Debug.Log($"[InsanityWorld] BuildAll: copied .pdb files for Debug into {outputDir}");
            }

            if (!CopyGameAssets(outputDir))
                return false;

            Debug.Log("[InsanityWorld] BuildAll: DONE.");
            return true;
        }

        public static bool CopyGameAssets(string outputDir)
        {
            var srcRoot = Path.Combine(Application.dataPath, "_game");
            if (!Directory.Exists(srcRoot))
            {
                Debug.Log($"[InsanityWorld] CopyGameAssets: no folder at {srcRoot}, nothing to copy");
                return true;
            }

            var scriptsRoot = Path.Combine(srcRoot, "Scripts") + Path.DirectorySeparatorChar;
            var claimedBy = new Dictionary<string, string>();
            int copied = 0;

            foreach (var file in Directory.GetFiles(srcRoot, "*", SearchOption.AllDirectories))
            {
                if (file.StartsWith(scriptsRoot, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var subDir = ResolveAssetSubDir(file);
                if (subDir == null)
                    continue;

                if (subDir.Length == 0)
                {
                    Debug.LogError($"[InsanityWorld] CopyGameAssets: cannot tell asset type of '{file}'. Expected character, quest grid config, grid config, dialogue or localization fragment.");
                    return false;
                }

                var fileName = Path.GetFileName(file);
                var key = Path.Combine(subDir, fileName);
                if (claimedBy.TryGetValue(key, out var taken))
                {
                    Debug.LogError($"[InsanityWorld] CopyGameAssets: '{key}' is produced twice:\n  {taken}\n  {file}\nRename one of them.");
                    return false;
                }

                claimedBy[key] = file;
                var dstDir = Path.Combine(outputDir, "Assets", subDir);
                Directory.CreateDirectory(dstDir);
                File.Copy(file, Path.Combine(dstDir, fileName), overwrite: true);
                copied++;
            }

            Debug.Log($"[InsanityWorld] CopyGameAssets: copied {copied} asset(s) into {Path.Combine(outputDir, "Assets")}");
            return true;
        }

        public static string ResolveAssetSubDir(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".yarn" || ext == ".csv")
                return "Dialogues";

            if (ext != ".json")
                return null;

            if (IsInTranslationsFolder(path))
                return null;

            return ResolveJsonSubDir(path);
        }

        public static bool IsInTranslationsFolder(string path)
        {
            var parent = Path.GetFileName(Path.GetDirectoryName(path));
            return string.Equals(parent, TRANSLATIONS_DIR_NAME, System.StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveJsonSubDir(string path)
        {
            Dictionary<string, object> fields;
            try
            {
                fields = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InsanityWorld] ResolveJsonSubDir: '{path}' is not valid JSON: {ex.Message}");
                return "";
            }

            if (fields == null)
                return "";

            if (fields.ContainsKey("yarnRootNode") || fields.ContainsKey("speakerNameKey"))
                return "Characters";

            if (fields.ContainsKey("questGridExitMode"))
                return Path.Combine("Quests", "GridConfigs");

            if (fields.ContainsKey("columns") && fields.ContainsKey("rows"))
                return "GridConfigs";

            return "";
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
