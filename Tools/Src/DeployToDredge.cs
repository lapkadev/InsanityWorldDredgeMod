namespace InsanityWorldMod.Tools;

public static partial class Constants
{
    public const string DEPLOY_MOD_ID = "lapkadev.InsanityWorldMod";
}

public static partial class Funcs
{
    public static int DeployToDredge()
    {
        int unityRc = BuildAll();
        if (unityRc != 0) return unityRc;

        var src = Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", G.buildConfig.ToString());
        if (string.IsNullOrEmpty(G.cfg.DredgeModsFolder))
        {
            LogError($"DredgeModsFolder not set in {CONFIG_FILE_NAME}");
            return 1;
        }
        var dst = Path.Combine(G.cfg.DredgeModsFolder, DEPLOY_MOD_ID);
        return DeployToDredge(src, dst, G.buildConfig);
    }

    public static int DeployToDredge(string src, string dst, BuildConfiguration config)
    {
        if (!Directory.Exists(src))
        {
            LogError($"Source folder doesn't exist: {src}");
            return 1;
        }

        try
        {
            if (Directory.Exists(dst)) Directory.Delete(dst, recursive: true);
            Directory.CreateDirectory(dst);
            var excludeExtensions = config == BuildConfiguration.Release
                ? new[] { ".pdb", ".manifest" }
                : new[] { ".manifest" };
            CopyDirectoryFiltered(src, dst, excludeExtensions, new[] { ".tmp" });
            LogInfo($"Deployed to: {dst}");
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Deploy failed: {ex}");
            return 1;
        }
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
