using System.Text.Json.Nodes;

namespace InsanityWorldMod.Tools;

public static partial class Funcs
{
    public static int BuildAll()
    {
        int bootRc = Bootstrap();
        if (bootRc != 0) return bootRc;

        var items = G.cfg.UnityEditorBuildItems;
        if (items.Length == 0)
        {
            LogError($"UnityEditorBuildItems is empty in {CONFIG_FILE_NAME}. Copy the block from {CONFIG_TEMPLATE_FILE_NAME}.");
            return 1;
        }

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
            var resolvedPath = Path.GetFullPath(Path.IsPathRooted(item.Path) ? item.Path : Path.Combine(G.repoRoot, item.Path)).ToLowerInvariant();
            if (!string.IsNullOrEmpty(G.skipUnityProject) && resolvedPath == G.skipUnityProject)
            {
                LogInfo($"======= Step {i + 1}/{items.Length}: SKIPPED (current Unity project): {item.Path} =======");
                continue;
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
}
