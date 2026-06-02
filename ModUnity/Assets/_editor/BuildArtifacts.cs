using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace InsanityWorldMod.EditorTools
{
    /// <summary>
    /// Build pipeline for InsanityWorldMod artifacts.
    ///
    /// Menu structure (priorities create separators when gap > 10):
    ///   InsanityWorld/
    ///     Build Core+Api+Bundles (Debug)               [1.1] Unity build only ==> mod-repo (includes .pdb)
    ///     Build Core+Api+Bundles (Release)             [1.2] Unity build only ==> mod-repo (no .pdb for Loader)
    ///     --- separator ---
    ///     Build Mod + Deploy to DREDGE (Debug)         [2.1] dotnet build mod-repo (no Unity rebuild)
    ///     Build Mod + Deploy to DREDGE (Release)       [2.2] + dotnet -c Release (no .pdb for Loader)
    ///     --- separator ---
    ///     Build All (Debug)   + Deploy to DREDGE       [3.1] full pipeline (Unity + dotnet)
    ///     Build All (Release) + Deploy to DREDGE       [3.2] full pipeline, Release config
    ///     --- separator ---
    ///     Bump Patch (x.y.Z)                           [4.1] edits mod_meta.json - sem-ver patch++
    ///     Bump Minor (x.Y.0)                           [4.2] edits mod_meta.json - sem-ver minor++
    ///     Bump Major (X.0.0)                           [4.3] edits mod_meta.json - sem-ver major++
    ///     --- separator ---
    ///     Build Release + Package for GitHub           [5.1] Release + zip in ReleaseArchives/
    /// </summary>
    public static class BuildArtifacts
    {
        // Auto-detect mod-repo root from Unity project location.
        // Application.dataPath = "<mod-repo>/ModUnity/Assets", so "../.." gives mod-repo root.
        // Works for any contributor regardless of where they cloned the repo.
        private static readonly string MOD_REPO_PATH =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")) + Path.DirectorySeparatorChar;

        // Layout: <mod-repo>/ModLoader/InsanityWorldMod/ contains Loader.csproj + mod_meta.json.
        private const string MOD_LOADER_FOLDER = "ModLoader";
        private const string MOD_FOLDER_NAME = "InsanityWorldMod";

        // Must match ModGUID in mod_meta.json.
        private const string MOD_ID = "lapkadev.InsanityWorldMod";

        // Per-machine config (gitignored via *.user), created on first deploy.
        private const string LOCAL_CONFIG_FILE = "local_dev.json.user";

        private const string API_ASSEMBLY_NAME = "InsanityWorldMod.Api";
        private const string CORE_ASSEMBLY_NAME = "InsanityWorldMod.Core";

        // ==================== Group 1: Unity build only ==> mod-repo ====================

        [MenuItem("InsanityWorld/Build Core+Api+Bundles (Debug)", false, 1)]
        public static void BuildCoreApiBundlesDebug()
        {
            BuildCoreApiBundlesInternal(release: false);
        }

        [MenuItem("InsanityWorld/Build Core+Api+Bundles (Release)", false, 2)]
        public static void BuildCoreApiBundlesRelease()
        {
            BuildCoreApiBundlesInternal(release: true);
        }

        // ==================== Group 2: dotnet build mod-repo ==> DREDGE ====================

        [MenuItem("InsanityWorld/Build Mod + Deploy to DREDGE (Debug)", false, 100)]
        public static void BuildModDeployDebug()
        {
            if (!RunDotnetBuild(release: false)) return;
            DeployBinToDredge(release: false);
        }

        [MenuItem("InsanityWorld/Build Mod + Deploy to DREDGE (Release)", false, 101)]
        public static void BuildModDeployRelease()
        {
            if (!RunDotnetBuild(release: true)) return;
            DeployBinToDredge(release: true);
        }

        // ==================== Group 3: Full pipeline (Unity + dotnet) ==> DREDGE ====================

        [MenuItem("InsanityWorld/Build All (Debug)   + Deploy to DREDGE", false, 200)]
        public static void BuildAllDebugDeploy()
        {
            if (!BuildCoreApiBundlesInternal(release: false)) return;
            if (!RunDotnetBuild(release: false)) return;
            DeployBinToDredge(release: false);
        }

        [MenuItem("InsanityWorld/Build All (Release) + Deploy to DREDGE", false, 201)]
        public static void BuildAllReleaseDeploy()
        {
            if (!BuildCoreApiBundlesInternal(release: true)) return;
            if (!RunDotnetBuild(release: true)) return;
            DeployBinToDredge(release: true);
        }

        // ==================== Group 4: Bump mod version (sem-ver) ====================

        [MenuItem("InsanityWorld/Bump Patch (x.y.Z)", false, 300)]
        public static void BumpPatch() => BumpVersionInternal(componentIndex: 2);

        [MenuItem("InsanityWorld/Bump Minor (x.Y.0)", false, 301)]
        public static void BumpMinor() => BumpVersionInternal(componentIndex: 1);

        [MenuItem("InsanityWorld/Bump Major (X.0.0)", false, 302)]
        public static void BumpMajor() => BumpVersionInternal(componentIndex: 0);

        // ==================== Group 5: Build Release + Package for GitHub ====================

        [MenuItem("InsanityWorld/Build Release + Package for GitHub", false, 400)]
        public static void BuildReleaseAndPackage()
        {
            if (!BuildCoreApiBundlesInternal(release: true)) return;
            if (!RunDotnetBuild(release: true)) return;
            PackageReleaseZipInternal();
        }

        // ==================== Internal helpers ====================

        /// <summary>
        /// Returns the staging output folder for the given config:
        /// .../ModLoader/InsanityWorldMod/bin/{Debug|Release}/.
        /// </summary>
        private static string GetBinFolder(bool release)
        {
            var configFolder = release ? "Release" : "Debug";
            return Path.Combine(MOD_REPO_PATH, MOD_LOADER_FOLDER, MOD_FOLDER_NAME, "bin", configFolder);
        }

        [System.Serializable]
        private class LocalConfig
        {
            public string dredgeModsFolder = "";
            public string unityExePath = @"C:/Program Files/Unity/Hub/Editor/2021.3.5f1/Editor/Unity.exe";
        }

        private static string LocalConfigPath => Path.Combine(MOD_REPO_PATH, LOCAL_CONFIG_FILE);

        private static LocalConfig LoadLocalConfig()
        {
            if (File.Exists(LocalConfigPath))
            {
                try { return JsonUtility.FromJson<LocalConfig>(File.ReadAllText(LocalConfigPath)) ?? new LocalConfig(); }
                catch { }
            }
            return new LocalConfig();
        }

        private static void SaveLocalConfig(LocalConfig cfg)
        {
            File.WriteAllText(LocalConfigPath, JsonUtility.ToJson(cfg, prettyPrint: true));
        }

        /// <summary>
        /// Returns <DREDGE>/Mods/<MOD_ID>. Prompts via folder picker if config
        /// is missing or path is invalid. Returns null on cancel.
        /// </summary>
        private static string GetDredgeDeployFolder()
        {
            var cfg = LoadLocalConfig();
            if (string.IsNullOrEmpty(cfg.dredgeModsFolder) || !Directory.Exists(cfg.dredgeModsFolder))
            {
                var picked = EditorUtility.OpenFolderPanel(
                    "Select DREDGE Mods folder (e.g. <DREDGE_install>/Mods)",
                    "", "");
                if (string.IsNullOrEmpty(picked))
                {
                    Debug.LogError("[InsanityWorld] DREDGE Mods folder not configured - aborting.");
                    return null;
                }
                cfg.dredgeModsFolder = picked;
                SaveLocalConfig(cfg);
                Debug.Log($"[InsanityWorld] Saved DREDGE Mods folder to {LocalConfigPath}");
            }
            return Path.Combine(cfg.dredgeModsFolder, MOD_ID);
        }

        /// <summary>
        /// Group 1: stages Api.dll + Core.dll + mod_meta.json + Localization + AssetBundles
        /// into bin/{Debug|Release}/. dotnet build (Group 2) adds Loader.dll on top.
        /// </summary>
        private static bool BuildCoreApiBundlesInternal(bool release)
        {
            if (!RefreshAndCheckReady(release ? "Build (Release)" : "Build (Debug)")) return false;
            if (!ValidateModRepoPath()) return false;

            EditorUtility.DisplayProgressBar("InsanityWorld Build", "Staging into bin/...", 0.2f);
            try
            {
                // --- Verify Unity-compiled DLLs exist ---
                var apiSrc = $"Library/ScriptAssemblies/{API_ASSEMBLY_NAME}.dll";
                var coreSrc = $"Library/ScriptAssemblies/{CORE_ASSEMBLY_NAME}.dll";

                if (!File.Exists(apiSrc) || !File.Exists(coreSrc))
                {
                    EditorUtility.DisplayDialog(
                        "InsanityWorld Build",
                        $"Source DLLs not found.\nExpected:\n  {apiSrc}\n  {coreSrc}\n\nMake sure scripts compile without errors first.",
                        "OK");
                    return false;
                }

                var binDir = GetBinFolder(release);
                Directory.CreateDirectory(binDir);
                var modProjectFolder = Path.Combine(MOD_REPO_PATH, MOD_LOADER_FOLDER, MOD_FOLDER_NAME);

                // --- Copy Api.dll + Core.dll into bin/<Config>/ ---
                File.Copy(apiSrc, Path.Combine(binDir, $"{API_ASSEMBLY_NAME}.dll"), overwrite: true);
                File.Copy(coreSrc, Path.Combine(binDir, $"{CORE_ASSEMBLY_NAME}.dll"), overwrite: true);

                // --- Copy mod_meta.json from csproj folder ---
                File.Copy(
                    Path.Combine(modProjectFolder, "mod_meta.json"),
                    Path.Combine(binDir, "mod_meta.json"),
                    overwrite: true);

                // --- Copy Localization JSONs from ModUnity/Assets/Localization/ ---
                EditorUtility.DisplayProgressBar("InsanityWorld Build", "Copying Localization...", 0.5f);
                var locSrc = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Assets", "Localization");
                if (Directory.Exists(locSrc))
                {
                    var locDst = Path.Combine(binDir, "Assets", "Localization");
                    Directory.CreateDirectory(locDst);
                    foreach (var jsonFile in Directory.GetFiles(locSrc, "*.json"))
                    {
                        File.Copy(jsonFile, Path.Combine(locDst, Path.GetFileName(jsonFile)), overwrite: true);
                    }
                }

                Debug.Log($"[InsanityWorld] {API_ASSEMBLY_NAME}.dll + {CORE_ASSEMBLY_NAME}.dll + mod_meta.json + Localization ({(release ? "Release" : "Debug")}) staged in: {binDir}");

                // --- Build AssetBundles into bin/<Config>/Assets/Bundles/ ---
                EditorUtility.DisplayProgressBar("InsanityWorld Build", "Building AssetBundles...", 0.8f);

                var bundlesDir = Path.Combine(binDir, "Assets", "Bundles");
                Directory.CreateDirectory(bundlesDir);

                BuildPipeline.BuildAssetBundles(
                    bundlesDir,
                    BuildAssetBundleOptions.None,
                    BuildTarget.StandaloneWindows64);

                Debug.Log($"[InsanityWorld] AssetBundles built to: {bundlesDir}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InsanityWorld] BuildCoreApiBundlesInternal failed: {ex}");
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Copies bin/{Debug|Release}/ contents to DREDGE/Mods/[ModGUID]/.
        /// </summary>
        private static bool DeployBinToDredge(bool release)
        {
            var binDir = GetBinFolder(release);
            if (!Directory.Exists(binDir))
            {
                Debug.LogError($"[InsanityWorld] bin folder doesn't exist: {binDir} - run Build first.");
                return false;
            }

            var deployDir = GetDredgeDeployFolder();
            if (string.IsNullOrEmpty(deployDir)) return false;

            try
            {
                if (Directory.Exists(deployDir))
                {
                    Directory.Delete(deployDir, recursive: true);
                }
                Directory.CreateDirectory(deployDir);
                CopyDirectoryFiltered(binDir, deployDir, new string[0], new string[0]);
                Debug.Log($"[InsanityWorld] Deployed to: {deployDir}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InsanityWorld] DeployBinToDredge failed: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Runs `dotnet build` on mod-repo with -c Debug or -c Release.
        /// Output goes to bin/$(Configuration)/
        /// </summary>
        private static bool RunDotnetBuild(bool release)
        {
            if (!RefreshAndCheckReady(release ? "Build Mod (Release)" : "Build Mod (Debug)")) return false;

            var modProjectFolder = Path.Combine(MOD_REPO_PATH, MOD_LOADER_FOLDER, MOD_FOLDER_NAME);

            var csprojFiles = Directory.GetFiles(modProjectFolder, "*.csproj");
            if (csprojFiles.Length == 0)
            {
                Debug.LogError($"[InsanityWorld] No .csproj found in {modProjectFolder}. Cannot run dotnet build.");
                return false;
            }

            var config = release ? "Release" : "Debug";
            EditorUtility.DisplayProgressBar("InsanityWorld Build", $"Running dotnet build -c {config}...", 0.9f);
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"build -c {config}")
                {
                    WorkingDirectory = modProjectFolder,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[InsanityWorld] dotnet build failed (exit code {process.ExitCode}).\nStdout:\n{output}\nStderr:\n{error}");
                    return false;
                }

                Debug.Log($"[InsanityWorld] dotnet build (-c {config}) done.\n{output}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InsanityWorld] RunDotnetBuild failed: {ex}");
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool ValidateModRepoPath()
        {
            if (string.IsNullOrEmpty(MOD_REPO_PATH) || !Directory.Exists(MOD_REPO_PATH))
            {
                EditorUtility.DisplayDialog(
                    "InsanityWorld Build",
                    $"MOD_REPO_PATH does not exist:\n{MOD_REPO_PATH}\n\nExpected auto-detect (mod-repo root = ModUnity/../..) to find the mod-repo root. Check that this Unity project is opened from ModUnity/ inside the mod-repo.",
                    "OK");
                return false;
            }
            return true;
        }

        // ==================== Bump version helpers ====================

        // Regex preserves original mod_meta.json formatting (tabs, key order) - JObject
        // round-trip would re-emit with default formatting and break diffs.
        private static readonly Regex ModMetaVersionRegex =
            new Regex(@"""Version""\s*:\s*""(\d+)\.(\d+)\.(\d+)""");

        // Matches the ModVersion.Current constant in ModUnity/Assets/_game/scripts/Core/Version.cs
        // - embedded into Core.dll's AssemblyVersion metadata at compile time.
        private static readonly Regex ModVersionConstRegex =
            new Regex(@"(public\s+const\s+string\s+Current\s*=\s*"")(\d+)\.(\d+)\.(\d+)("")");

        /// <summary>
        /// componentIndex: 0=major, 1=minor, 2=patch. Resets less-significant components to 0.
        /// Updates BOTH mod_meta.json (ModLoader/InsanityWorldMod/) AND Version.cs (ModUnity/Assets/_game/scripts/Core/).
        /// </summary>
        private static void BumpVersionInternal(int componentIndex)
        {
            if (!ValidateModRepoPath()) return;

            var metaPath = Path.Combine(MOD_REPO_PATH, MOD_LOADER_FOLDER, MOD_FOLDER_NAME, "mod_meta.json");
            var modUnityRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var versionCsPath = Path.Combine(modUnityRoot, "Assets", "_game", "scripts", "Core", "Version.cs");

            if (!File.Exists(metaPath))
            {
                EditorUtility.DisplayDialog("InsanityWorld Bump Version",
                    $"mod_meta.json not found at:\n{metaPath}", "OK");
                return;
            }
            if (!File.Exists(versionCsPath))
            {
                EditorUtility.DisplayDialog("InsanityWorld Bump Version",
                    $"Version.cs not found at:\n{versionCsPath}", "OK");
                return;
            }

            // Read both files + parse current version, verify they match.
            var json = File.ReadAllText(metaPath);
            var metaMatch = ModMetaVersionRegex.Match(json);
            if (!metaMatch.Success)
            {
                EditorUtility.DisplayDialog("InsanityWorld Bump Version",
                    $"\"Version\" field with x.y.z format not found in mod_meta.json.\nCurrent contents:\n{json}", "OK");
                return;
            }

            var versionCs = File.ReadAllText(versionCsPath);
            var csMatch = ModVersionConstRegex.Match(versionCs);
            if (!csMatch.Success)
            {
                EditorUtility.DisplayDialog("InsanityWorld Bump Version",
                    $"public const string Current = \"x.y.z\" not found in Version.cs.\nFile:\n{versionCsPath}", "OK");
                return;
            }

            var metaVersion = $"{metaMatch.Groups[1].Value}.{metaMatch.Groups[2].Value}.{metaMatch.Groups[3].Value}";
            var csVersion = $"{csMatch.Groups[2].Value}.{csMatch.Groups[3].Value}.{csMatch.Groups[4].Value}";
            if (metaVersion != csVersion)
            {
                if (!EditorUtility.DisplayDialog("InsanityWorld Bump Version",
                    $"Version mismatch detected!\n\nmod_meta.json: {metaVersion}\nVersion.cs:    {csVersion}\n\nBump will use mod_meta.json's value ({metaVersion}) as the source and overwrite Version.cs. Continue?",
                    "Bump anyway", "Cancel"))
                {
                    return;
                }
            }

            var parts = new[]
            {
                int.Parse(metaMatch.Groups[1].Value),
                int.Parse(metaMatch.Groups[2].Value),
                int.Parse(metaMatch.Groups[3].Value),
            };
            var oldVersion = $"{parts[0]}.{parts[1]}.{parts[2]}";

            parts[componentIndex]++;
            for (int i = componentIndex + 1; i < parts.Length; i++) parts[i] = 0;
            var newVersion = $"{parts[0]}.{parts[1]}.{parts[2]}";

            var componentName = componentIndex == 0 ? "Major" : componentIndex == 1 ? "Minor" : "Patch";

            if (!EditorUtility.DisplayDialog($"InsanityWorld Bump {componentName}",
                $"Bump version from {oldVersion} to {newVersion}?\n\nUpdates BOTH:\n  - {metaPath}\n  - {versionCsPath}",
                "Bump", "Cancel"))
            {
                return;
            }

            // Write back to both files.
            var updatedJson = ModMetaVersionRegex.Replace(json, $"\"Version\": \"{newVersion}\"");
            File.WriteAllText(metaPath, updatedJson);

            var updatedCs = ModVersionConstRegex.Replace(versionCs, $"${{1}}{newVersion}${{5}}");
            File.WriteAllText(versionCsPath, updatedCs);

            // Force Unity to detect the Version.cs change and recompile Core/Api assemblies
            // immediately. Without this, a subsequent Build menu click would copy the stale
            // (pre-bump) DLLs out of Library/ScriptAssemblies/. Compile happens async - user
            // must wait for it before the next build (the Build menus also guard against
            // EditorApplication.isCompiling).
            AssetDatabase.Refresh();

            Debug.Log($"[InsanityWorld] Version bumped: {oldVersion} ==> {newVersion} (mod_meta.json + Version.cs). Waiting for Unity to recompile assemblies - watch bottom-right spinner.");
        }

        /// <summary>
        /// Triggers AssetDatabase.Refresh() to make Unity detect any external file
        /// changes (e.g. Version.cs edited by Bump menu), then aborts if a compile / asset
        /// update is in flight. Returns true only when ready to safely read Library/ScriptAssemblies/.
        /// Build menus call this first to avoid packaging stale DLLs.
        /// Refresh is idempotent - no double-compile if nothing changed.
        /// </summary>
        private static bool RefreshAndCheckReady(string menuLabel)
        {
            AssetDatabase.Refresh();
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog($"InsanityWorld {menuLabel}",
                    "Unity is processing changes / recompiling assemblies (watch the bottom-right spinner).\nWait for it to finish, then retry - otherwise the build would pick up stale DLLs.",
                    "OK");
                return false;
            }
            return true;
        }

        // ==================== Package Release ZIP helpers ====================

        private static readonly string[] ExcludedExtensions = { ".pdb", ".meta" };

        // Folder names to skip when staging the release zip. 
        // - "saves" is created by the mod at runtime
        private static readonly string[] ExcludedDirNames = { "saves" };

        /// <summary>
        /// Packages bin/Release/ contents into a versioned ZIP for GitHub Release.
        /// </summary>
        private static bool PackageReleaseZipInternal()
        {
            // Source: bin/Release/ (populated by Group 1 + Group 2 of "Build Release + Package" menu).
            var binDir = GetBinFolder(release: true);
            if (!Directory.Exists(binDir))
            {
                EditorUtility.DisplayDialog("InsanityWorld Package",
                    $"bin/Release/ folder not found:\n{binDir}\n\nRun the full Build Release pipeline first.", "OK");
                return false;
            }

            // Read version from mod_meta.json (source-of-truth in csproj folder).
            var metaPath = Path.Combine(MOD_REPO_PATH, MOD_LOADER_FOLDER, MOD_FOLDER_NAME, "mod_meta.json");
            var json = File.ReadAllText(metaPath);
            var match = ModMetaVersionRegex.Match(json);
            if (!match.Success)
            {
                EditorUtility.DisplayDialog("InsanityWorld Package",
                    "Cannot read Version from mod_meta.json.", "OK");
                return false;
            }
            var version = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";

            // Output: <mod-repo>/ReleaseArchives/v<version>/
            var releaseDir = Path.Combine(MOD_REPO_PATH, "ReleaseArchives", $"v{version}");
            Directory.CreateDirectory(releaseDir);

            // Stage: clean copy with filters
            var stageDir = Path.Combine(releaseDir, MOD_ID);
            if (Directory.Exists(stageDir)) Directory.Delete(stageDir, recursive: true);
            CopyDirectoryFiltered(binDir, stageDir, ExcludedExtensions, ExcludedDirNames);

            // Zip stage into InsanityWorld.zip in
            // <mod-repo>/ReleaseArchives/v<version>/
            var zipPath = Path.Combine(releaseDir, "InsanityWorld.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            // includeBaseDirectory: false - files end up at zip root
            ZipFile.CreateFromDirectory(stageDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            Debug.Log($"[InsanityWorld] Release ZIP: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
            return true;
        }

        private static void CopyDirectoryFiltered(string src, string dst, string[] excludeExtensions, string[] excludeDirNames)
        {
            Directory.CreateDirectory(dst);
            foreach (var filePath in Directory.GetFiles(src))
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (excludeExtensions.Contains(ext)) continue;
                File.Copy(filePath, Path.Combine(dst, Path.GetFileName(filePath)));
            }
            foreach (var subDir in Directory.GetDirectories(src))
            {
                var dirName = Path.GetFileName(subDir);
                if (excludeDirNames.Contains(dirName)) continue;
                var dstSubDir = Path.Combine(dst, dirName);
                CopyDirectoryFiltered(subDir, dstSubDir, excludeExtensions, excludeDirNames);
            }
        }
    }
}
