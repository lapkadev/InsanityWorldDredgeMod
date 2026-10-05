using System.Text.Json.Nodes;

namespace InsW.Tools;

public static partial class Constants
{
    public const string BUILD_UNITY_PROJECT_REL_DIR  = "ModUnity";
    public const string BUILD_UNITY_METHOD           = "InsW.Editor.Funcs.BuildAll";
    public const string BUILD_LOADER_CSPROJ_REL_PATH = "ModLoader/InsanityWorldMod/InsanityWorldMod.csproj";
}

public static partial class Funcs
{
    public static int BuildAll()
    {
        int bootRc = Bootstrap();
        if (bootRc != 0) return bootRc;

        var buildBinDir = Path.GetFullPath(Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", G.buildConfig.ToString()));
        if (Directory.Exists(buildBinDir))
        {
            Directory.Delete(buildBinDir, recursive: true);
            LogInfo($"BuildAll: cleaned {buildBinDir}");
        }

        var args = new JsonObject
        {
            ["BuildDir"] = Path.GetFullPath(Path.Combine(G.repoRoot, G.cfg.BuildDir)),
            ["BuildConfiguration"] = G.buildConfig.ToString(),
        };

        var unityProjectDir = Path.GetFullPath(Path.Combine(G.repoRoot, BUILD_UNITY_PROJECT_REL_DIR));
        LogInfo($"======= Unity build: {unityProjectDir} =======");
        int unityRc = UnityRun(unityProjectDir, BUILD_UNITY_METHOD, args);
        if (unityRc != 0) { LogError($"FAILED at '{unityProjectDir}' (exit {unityRc}). Aborting."); return unityRc; }

        LogInfo($"======= Loader build: {BUILD_LOADER_CSPROJ_REL_PATH} =======");
        int loaderRc = DotNetRun(BUILD_LOADER_CSPROJ_REL_PATH);
        if (loaderRc != 0) { LogError($"FAILED at '{BUILD_LOADER_CSPROJ_REL_PATH}' (exit {loaderRc}). Aborting."); return loaderRc; }

        return 0;
    }
}
