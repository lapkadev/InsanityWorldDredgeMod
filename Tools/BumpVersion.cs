using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace InsanityWorldMod.Tools;

public static class BumpVersion
{
    private record VersionFile(string Path, string Type);

    private static readonly Dictionary<string, Regex> Regexes = new()
    {
        ["modMeta"]              = new(@"""Version""\s*:\s*""(\d+)\.(\d+)\.(\d+)"""),
        ["csharpConst"]          = new(@"(public\s+const\s+string\s+Current\s*=\s*"")(\d+)\.(\d+)\.(\d+)("")"),
        ["unityProjectSettings"] = new(@"(bundleVersion:\s*)\S+"),
    };

    // componentIndex: 0=major, 1=minor, 2=patch
    public static int Run(int componentIndex)
    {
        string repoRoot = RepoLocator.FindModRepoRoot();
        var cfg = LocalConfig.Load(repoRoot);
        var files = LoadVersionFiles(cfg);

        if (files == null)
        {
            Console.Error.WriteLine("versionFiles array missing in local_dev.json.user. Copy the block from local_dev.json.user.template.");
            return 1;
        }
        if (files.Length == 0)
        {
            Console.Error.WriteLine("versionFiles is empty - nothing to bump.");
            return 1;
        }

        var resolved = files.Select(f => (file: f, abs: ResolveAbs(repoRoot, f.Path))).ToArray();

        foreach (var (file, abs) in resolved)
        {
            if (!Regexes.ContainsKey(file.Type))
            {
                Console.Error.WriteLine($"UNKNOWN type '{file.Type}' for {file.Path}. Supported: {string.Join(", ", Regexes.Keys)}");
                return 1;
            }
            if (!File.Exists(abs))
            {
                Console.Error.WriteLine($"NOT FOUND: {abs}");
                return 1;
            }
        }

        var sourceFile = resolved[0];
        var sourceMatch = ReadVersion(sourceFile.abs, sourceFile.file.Type);
        if (sourceMatch == null)
        {
            Console.Error.WriteLine($"Version not found in source file: {sourceFile.abs}");
            return 1;
        }

        int[] parts = sourceMatch;
        string oldVer = $"{parts[0]}.{parts[1]}.{parts[2]}";
        parts[componentIndex]++;
        for (int i = componentIndex + 1; i < parts.Length; i++) parts[i] = 0;
        string newVer = $"{parts[0]}.{parts[1]}.{parts[2]}";

        Console.WriteLine($"Bumping {oldVer} -> {newVer} (source: {sourceFile.file.Path})");

        foreach (var (file, abs) in resolved)
        {
            if (!WriteVersion(abs, file.Type, newVer))
            {
                Console.Error.WriteLine($"FAILED to write version into {abs}");
                return 1;
            }
            Console.WriteLine($"OK  {abs}");
        }

        return 0;
    }

    private static VersionFile[]? LoadVersionFiles(JsonObject cfg)
    {
        if (!cfg.TryGetPropertyValue("versionFiles", out var node) || node is not JsonArray arr) return null;
        var result = new List<VersionFile>();
        foreach (var item in arr)
        {
            if (item is not JsonObject obj) continue;
            var path = obj["path"]?.GetValue<string>();
            var type = obj["type"]?.GetValue<string>();
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(type)) continue;
            result.Add(new VersionFile(path, type));
        }
        return result.ToArray();
    }

    private static string ResolveAbs(string repoRoot, string relPath)
    {
        var normalized = relPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(repoRoot, normalized));
    }

    private static int[]? ReadVersion(string absPath, string type)
    {
        var content = File.ReadAllText(absPath);
        var match = Regexes[type].Match(content);
        if (!match.Success) return null;

        return type switch
        {
            "modMeta"              => new[] { int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value) },
            "csharpConst"          => new[] { int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value) },
            "unityProjectSettings" => ParseSimpleVersion(match.Value),
            _                      => null,
        };
    }

    private static bool WriteVersion(string absPath, string type, string newVer)
    {
        var content = File.ReadAllText(absPath);
        var regex = Regexes[type];
        if (!regex.IsMatch(content)) return false;

        var replaced = type switch
        {
            "modMeta"              => regex.Replace(content, $"\"Version\": \"{newVer}\""),
            "csharpConst"          => regex.Replace(content, $"${{1}}{newVer}${{5}}"),
            "unityProjectSettings" => regex.Replace(content, $"${{1}}{newVer}"),
            _                      => content,
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
