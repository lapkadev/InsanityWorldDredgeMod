using System.Text.RegularExpressions;

namespace InsanityWorldMod.Tools;

public static class BumpVersion
{
    private const string ModMetaRelPath = "ModLoader/InsanityWorldMod/mod_meta.json";
    private const string VersionCsRelPath = "ModUnity/Assets/_game/Scripts/Core/Version.cs";

    private static readonly Regex ModMetaRegex = new(@"""Version""\s*:\s*""(\d+)\.(\d+)\.(\d+)""");
    private static readonly Regex VersionCsRegex = new(@"(public\s+const\s+string\s+Current\s*=\s*"")(\d+)\.(\d+)\.(\d+)("")");

    // componentIndex: 0=major, 1=minor, 2=patch
    public static int Run(int componentIndex)
    {
        string repoRoot = RepoLocator.FindModRepoRoot();
        string metaPath = Path.Combine(repoRoot, ModMetaRelPath.Replace('/', Path.DirectorySeparatorChar));
        string csPath   = Path.Combine(repoRoot, VersionCsRelPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(metaPath)) { Console.Error.WriteLine($"NOT FOUND: {metaPath}"); return 1; }
        if (!File.Exists(csPath))   { Console.Error.WriteLine($"NOT FOUND: {csPath}");   return 1; }

        string json = File.ReadAllText(metaPath);
        var metaMatch = ModMetaRegex.Match(json);
        if (!metaMatch.Success) { Console.Error.WriteLine($"Version field not found in {metaPath}"); return 1; }

        string cs = File.ReadAllText(csPath);
        var csMatch = VersionCsRegex.Match(cs);
        if (!csMatch.Success) { Console.Error.WriteLine($"Version constant not found in {csPath}"); return 1; }

        string metaVer = $"{metaMatch.Groups[1]}.{metaMatch.Groups[2]}.{metaMatch.Groups[3]}";
        string csVer   = $"{csMatch.Groups[2]}.{csMatch.Groups[3]}.{csMatch.Groups[4]}";
        if (metaVer != csVer)
            Console.WriteLine($"Version mismatch (mod_meta.json={metaVer}, Version.cs={csVer}) - using mod_meta.json as source.");

        int[] parts = { int.Parse(metaMatch.Groups[1].Value), int.Parse(metaMatch.Groups[2].Value), int.Parse(metaMatch.Groups[3].Value) };
        string oldVer = $"{parts[0]}.{parts[1]}.{parts[2]}";
        parts[componentIndex]++;
        for (int i = componentIndex + 1; i < parts.Length; i++) parts[i] = 0;
        string newVer = $"{parts[0]}.{parts[1]}.{parts[2]}";

        Console.WriteLine($"Bumping {oldVer} -> {newVer}");

        File.WriteAllText(metaPath, ModMetaRegex.Replace(json, $"\"Version\": \"{newVer}\""));
        Console.WriteLine($"OK  {metaPath}");

        File.WriteAllText(csPath, VersionCsRegex.Replace(cs, $"${{1}}{newVer}${{5}}"));
        Console.WriteLine($"OK  {csPath}");

        return 0;
    }
}
