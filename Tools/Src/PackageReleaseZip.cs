using System.IO.Compression;
using System.Text.RegularExpressions;

namespace InsanityWorldMod.Tools;

public static partial class Constants
{
    public const string PACKAGE_MOD_META_REL_PATH      = "ModLoader/InsanityWorldMod/mod_meta.json";
    public const string PACKAGE_ARCHIVES_DIR_NAME      = "ReleaseArchives";
    public const string PACKAGE_ZIP_FILE_NAME          = "InsanityWorld.zip";
}

public static partial class Funcs
{
    private static readonly string[] PackageExcludedExtensions = { ".pdb", ".meta" };
    private static readonly string[] PackageExcludedDirNames = { "saves", ".tmp" };
    private static readonly Regex PackageModMetaVersionRegex = new(@"""Version""\s*:\s*""(\d+)\.(\d+)\.(\d+)""");

    public static int ReleaseZip()
    {
        int unityRc = BuildAll();
        if (unityRc != 0) return unityRc;

        var binDir = Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", BuildConfiguration.Release.ToString());

        if (!Directory.Exists(binDir))
        {
            LogError($"{G.cfg.BuildDir}/bin/Release folder not found: {binDir}");
            return 1;
        }

        var metaPath = Path.Combine(G.repoRoot, PACKAGE_MOD_META_REL_PATH.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(metaPath))
        {
            LogError($"mod_meta.json not found: {metaPath}");
            return 1;
        }

        var match = PackageModMetaVersionRegex.Match(File.ReadAllText(metaPath));
        if (!match.Success)
        {
            LogError("Cannot read Version from mod_meta.json.");
            return 1;
        }
        var version = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";

        var releaseDir = Path.Combine(G.repoRoot, G.cfg.BuildDir, PACKAGE_ARCHIVES_DIR_NAME, $"v{version}");
        Directory.CreateDirectory(releaseDir);
        var zipPath = Path.Combine(releaseDir, PACKAGE_ZIP_FILE_NAME);

        try
        {
            ZipDirectoryFiltered(binDir, zipPath, PackageExcludedExtensions, PackageExcludedDirNames);
            LogInfo($"Release ZIP: {zipPath}");
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Package failed: {ex}");
            return 1;
        }
    }

    // Recursively zips sourceDir into zipPath. Skips files with excluded extensions
    // and directories with excluded names. Adds entries directly without a staging copy.
    private static void ZipDirectoryFiltered(string sourceDir, string zipPath, string[] excludeExtensions, string[] excludeDirNames)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddDirectoryToArchive(archive, sourceDir, "", excludeExtensions, excludeDirNames);
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string srcDir, string entryPrefix, string[] excludeExtensions, string[] excludeDirNames)
    {
        foreach (var filePath in Directory.GetFiles(srcDir))
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (excludeExtensions.Contains(ext)) continue;
            var entryName = string.IsNullOrEmpty(entryPrefix)
                ? Path.GetFileName(filePath)
                : entryPrefix + "/" + Path.GetFileName(filePath);
            archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);
        }
        foreach (var subDir in Directory.GetDirectories(srcDir))
        {
            var dirName = Path.GetFileName(subDir);
            if (excludeDirNames.Contains(dirName)) continue;
            var nestedPrefix = string.IsNullOrEmpty(entryPrefix) ? dirName : entryPrefix + "/" + dirName;
            AddDirectoryToArchive(archive, subDir, nestedPrefix, excludeExtensions, excludeDirNames);
        }
    }
}
