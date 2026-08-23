using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InsanityWorldMod.Tools;

public static partial class Constants
{
    public const string TRANSLATIONS_DIR_NAME = "tr";
}

public static partial class Funcs
{
    private static readonly JsonSerializerOptions LocalizationWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static int MergeLocalization()
    {
        var gameDir = Path.Combine(G.repoRoot, "ModUnity", "Assets", "_game");
        var fragments = CollectLocalizationFragments(gameDir);
        if (fragments.Length == 0)
        {
            LogError($"MergeLocalization: no fragments found under {gameDir} in '{TRANSLATIONS_DIR_NAME}' folders");
            return 1;
        }

        var byLocale = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var fragment in fragments)
        {
            var locale = LocaleFromFragmentName(fragment);
            if (locale.Length == 0)
            {
                LogError($"MergeLocalization: cannot read locale from file name '{Path.GetFileName(fragment)}'");
                return 1;
            }

            JsonObject? parsed;
            try
            {
                parsed = JsonNode.Parse(File.ReadAllText(fragment)) as JsonObject;
            }
            catch (JsonException ex)
            {
                LogError($"MergeLocalization: '{fragment}' is not valid JSON: {ex.Message}");
                return 1;
            }

            if (parsed == null)
            {
                LogError($"MergeLocalization: '{fragment}' must contain a JSON object");
                return 1;
            }

            if (!byLocale.TryGetValue(locale, out var target))
            {
                target = new JsonObject();
                byLocale[locale] = target;
            }

            foreach (var entry in parsed)
            {
                if (target.ContainsKey(entry.Key))
                    LogInfo($"MergeLocalization: key '{entry.Key}' in '{Path.GetFileName(fragment)}' overrides an earlier fragment of locale '{locale}'");

                target[entry.Key] = entry.Value?.DeepClone();
            }
        }

        var dstDir = Path.Combine(G.repoRoot, G.cfg.BuildDir, "bin", G.buildConfig.ToString(), "Assets", "Localization");
        Directory.CreateDirectory(dstDir);
        foreach (var stale in Directory.GetFiles(dstDir, "*.json"))
            File.Delete(stale);

        foreach (var pair in byLocale.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var path = Path.Combine(dstDir, $"{pair.Key}.json");
            File.WriteAllText(path, pair.Value.ToJsonString(LocalizationWriteOptions));
        }

        LogInfo($"MergeLocalization: merged {fragments.Length} fragment(s) into {byLocale.Count} locale file(s) at {dstDir}");
        return 0;
    }

    public static string[] CollectLocalizationFragments(string gameDir)
    {
        if (!Directory.Exists(gameDir))
            return Array.Empty<string>();

        return Directory
            .GetFiles(gameDir, "*.json", SearchOption.AllDirectories)
            .Where(IsInTranslationsFolder)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    public static bool IsInTranslationsFolder(string path)
    {
        var parent = Path.GetFileName(Path.GetDirectoryName(path));
        return string.Equals(parent, TRANSLATIONS_DIR_NAME, StringComparison.OrdinalIgnoreCase);
    }

    private static string LocaleFromFragmentName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        int dot = name.LastIndexOf('.');
        return dot < 0 ? name : name[(dot + 1)..];
    }
}
