using System.Text.Json;
using System.Text.Json.Nodes;

namespace InsanityWorldMod.Tools;

internal static class LocalConfig
{
    private const string FileName = "local_dev.json.user";

    public static JsonObject Load(string repoRoot)
    {
        var path = Path.Combine(repoRoot, FileName);
        if (!File.Exists(path)) return new JsonObject();
        try { return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject(); }
        catch { return new JsonObject(); }
    }

    public static void Save(string repoRoot, JsonObject cfg)
    {
        var path = Path.Combine(repoRoot, FileName);
        File.WriteAllText(path, cfg.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static string? GetString(JsonObject cfg, string key)
    {
        return cfg.TryGetPropertyValue(key, out var node) ? node?.GetValue<string>() : null;
    }

    public static void SetString(JsonObject cfg, string key, string value)
    {
        cfg[key] = value;
    }
}
