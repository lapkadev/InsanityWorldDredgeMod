using System.Text.Json.Nodes;

namespace InsanityWorldMod.Tools;

public static partial class Funcs
{
    public static int BuildAll()
    {
        int bootRc = Bootstrap();
        if (bootRc != 0) return bootRc;

        var buildBinDir = Path.GetFullPath(Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", G.buildConfig.ToString()));
        if (!G.skipCleanBuildDir && Directory.Exists(buildBinDir))
        {
            Directory.Delete(buildBinDir, recursive: true);
            LogInfo($"BuildAll: cleaned {buildBinDir}");
        }

        int eosRc = BuildEosSdk();
        if (eosRc != 0) return eosRc;

        var rawItems = G.cfg.UnityEditorBuildItems;
        if (rawItems.Length == 0)
        {
            LogError($"UnityEditorBuildItems is empty in {CONFIG_FILE_NAME}. Copy the block from {CONFIG_TEMPLATE_FILE_NAME}.");
            return 1;
        }

        var modUnityResolved = Path.GetFullPath(Path.Combine(G.repoRoot, "ModUnity")).ToLowerInvariant();
        var items = rawItems
            .OrderBy(item => ResolveItemPath(item.Path) == modUnityResolved ? 0 : 1)
            .ToArray();

        LogInfo($"UnityEditorBuildItems to process ({items.Length}):");
        for (int i = 0; i < items.Length; i++) LogInfo($"  [{i + 1}] Path={items[i].Path}  Method={items[i].Method}");

        var args = new JsonObject
        {
            ["BuildDir"] = Path.GetFullPath(Path.Combine(G.repoRoot, G.cfg.BuildDir)),
            ["BuildConfiguration"] = G.buildConfig.ToString(),
        };

        int processed = 0;
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var resolvedPath = ResolveItemPath(item.Path);

            if (!string.IsNullOrEmpty(G.skipUnityProject) && resolvedPath == G.skipUnityProject)
            {
                LogInfo($"======= Step {i + 1}/{items.Length}: SKIPPED (current Unity project): {item.Path} =======");
                continue;
            }

            if (resolvedPath != modUnityResolved)
            {
                SyncCoreDllToProject(resolvedPath);
            }

            LogInfo($"======= Step {i + 1}/{items.Length}: {item.Path} =======");
            int rc = UnityRun(item.Path, item.Method, args);
            if (rc != 0) { LogError($"FAILED at '{item.Path}' (exit {rc}). Aborting."); return rc; }
            processed++;
        }
        LogInfo($"All {processed} Unity build(s) completed successfully (skipped {items.Length - processed}).");

        var dotnetItems = G.cfg.DotNetBuildItems;
        if (dotnetItems.Length > 0)
        {
            LogInfo($"DotNetBuildItems to process ({dotnetItems.Length}):");
            for (int i = 0; i < dotnetItems.Length; i++) LogInfo($"  [{i + 1}] Path={dotnetItems[i].Path}");

            for (int i = 0; i < dotnetItems.Length; i++)
            {
                var item = dotnetItems[i];
                LogInfo($"======= DotNet Step {i + 1}/{dotnetItems.Length}: {item.Path} =======");
                int rc = DotNetRun(item.Path);
                if (rc != 0) { LogError($"FAILED at '{item.Path}' (exit {rc}). Aborting."); return rc; }
            }
            LogInfo($"All {dotnetItems.Length} DotNet build(s) completed successfully.");
        }

        return 0;
    }

    private static int BuildEosSdk()
    {
        var sdkDir = G.cfg.EosSdkDir;
        if (string.IsNullOrEmpty(sdkDir))
        {
            LogError($"EosSdkDir is not set in {CONFIG_FILE_NAME}. Download the EOS C# SDK v1.18.1.2 (last version with 32-bit Win32) from the Epic Dev Portal, extract it, and set EosSdkDir to its root.");
            return 1;
        }
        if (!Directory.Exists(Path.Combine(sdkDir, "SDK", "Source")))
        {
            LogError($"EOS SDK source not found at '{sdkDir}\\SDK\\Source'. Check EosSdkDir in {CONFIG_FILE_NAME}.");
            return 1;
        }

        const string csproj = "EosSdk/InsanityWorldMod.EosSdk.csproj";
        LogInfo($"======= EOS SDK build: {csproj} =======");
        System.Environment.SetEnvironmentVariable("EosSdkDir", sdkDir);
        int rc = DotNetRun(csproj);
        if (rc != 0) { LogError($"FAILED building {csproj} (exit {rc}). Aborting."); return rc; }

        var builtDll = Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", G.buildConfig.ToString(), "EOSSDK.dll");
        if (!File.Exists(builtDll))
        {
            LogError($"EOSSDK.dll was not produced at {builtDll}.");
            return 1;
        }

        var dstDir = Path.Combine(G.repoRoot, "ModUnity", "Assets", "Plugins");
        Directory.CreateDirectory(dstDir);
        var dst = Path.Combine(dstDir, "EOSSDK.dll");
        File.Copy(builtDll, dst, overwrite: true);
        LogInfo($"Synced EOSSDK.dll -> {dst}");
        return 0;
    }

    private static string ResolveItemPath(string path)
    {
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(G.repoRoot, path)).ToLowerInvariant();
    }

    private static void SyncCoreDllToProject(string projectPath)
    {
        var src = Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", G.buildConfig.ToString(), "InsanityWorldMod.Core.dll");
        if (!File.Exists(src))
        {
            LogError($"Core.dll not found at {src} - cannot sync to {projectPath}/Assets/Plugins/. ModUnity must be built first.");
            return;
        }
        var dstDir = Path.Combine(projectPath, "Assets", "Plugins");
        Directory.CreateDirectory(dstDir);
        var dst = Path.Combine(dstDir, "InsanityWorldMod.Core.dll");
        File.Copy(src, dst, overwrite: true);
        LogInfo($"Synced Core.dll -> {dst}");
    }
}
