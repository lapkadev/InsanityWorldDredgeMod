using System.Text.RegularExpressions;

namespace InsanityWorldMod.Tools;

public static partial class Funcs
{
    private static readonly Dictionary<VersionFileType, Regex> Regexes = new()
    {
        [VersionFileType.ModMeta]              = new(@"""Version""\s*:\s*""(\d+)\.(\d+)\.(\d+)"""),
        [VersionFileType.CsharpConst]          = new(@"(public\s+const\s+string\s+Current\s*=\s*"")(\d+)\.(\d+)\.(\d+)("")"),
        [VersionFileType.UnityProjectSettings] = new(@"(bundleVersion:\s*)\S+"),
    };

    // componentIndex: 0=major, 1=minor, 2=patch
    public static int BumpVersion(int componentIndex)
    {
        var files = G.cfg.VersionFiles;
        if (files.Length == 0)
        {
            LogError($"VersionFiles is empty in {CONFIG_FILE_NAME}. Copy the block from {CONFIG_TEMPLATE_FILE_NAME}.");
            return 1;
        }

        var resolved = files.Select(f => (file: f, abs: ResolveAbs(G.repoRoot, f.Path))).ToArray();

        foreach (var (file, abs) in resolved)
        {
            if (!Regexes.ContainsKey(file.Type))
            {
                LogError($"UNKNOWN type '{file.Type}' for {file.Path}. Supported: {string.Join(", ", Regexes.Keys)}");
                return 1;
            }
            if (!File.Exists(abs))
            {
                LogError($"NOT FOUND: {abs}");
                return 1;
            }
        }

        var sourceFile = resolved[0];
        var sourceMatch = ReadVersion(sourceFile.abs, sourceFile.file.Type);
        if (sourceMatch == null)
        {
            LogError($"Version not found in source file: {sourceFile.abs}");
            return 1;
        }

        int[] parts = sourceMatch;
        string oldVer = $"{parts[0]}.{parts[1]}.{parts[2]}";
        parts[componentIndex]++;
        for (int i = componentIndex + 1; i < parts.Length; i++) parts[i] = 0;
        string newVer = $"{parts[0]}.{parts[1]}.{parts[2]}";

        LogInfo($"Bumping {oldVer} -> {newVer} (source: {sourceFile.file.Path})");

        foreach (var (file, abs) in resolved)
        {
            if (!WriteVersion(abs, file.Type, newVer))
            {
                LogError($"FAILED to write version into {abs}");
                return 1;
            }
            LogInfo($"OK  {abs}");
        }

        return 0;
    }

    private static string ResolveAbs(string repoRoot, string relPath)
    {
        var normalized = relPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(repoRoot, normalized));
    }

    private static int[]? ReadVersion(string absPath, VersionFileType type)
    {
        var content = File.ReadAllText(absPath);
        var match = Regexes[type].Match(content);
        if (!match.Success) return null;

        return type switch
        {
            VersionFileType.ModMeta              => new[] { int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) },
            VersionFileType.CsharpConst          => new[] { int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value) },
            VersionFileType.UnityProjectSettings => ParseSimpleVersion(match.Value),
            _                                    => null,
        };
    }

    private static bool WriteVersion(string absPath, VersionFileType type, string newVer)
    {
        var content = File.ReadAllText(absPath);
        var regex = Regexes[type];
        if (!regex.IsMatch(content)) return false;

        var replaced = type switch
        {
            VersionFileType.ModMeta              => regex.Replace(content, $"\"Version\": \"{newVer}\""),
            VersionFileType.CsharpConst          => regex.Replace(content, $"${{1}}{newVer}${{5}}"),
            VersionFileType.UnityProjectSettings => regex.Replace(content, $"${{1}}{newVer}"),
            _                                    => content,
        };
        File.WriteAllText(absPath, replaced);
        return true;
    }

    private static int[]? ParseSimpleVersion(string matched)
    {
        var verPart = matched.Split(':').Last().Trim();
        var split = verPart.Split('.');
        if (split.Length != 3) return null;
        return new[] { int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]) };
    }
}
