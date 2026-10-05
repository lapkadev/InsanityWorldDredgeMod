using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsW.Tools;

public class Config
{
    public string UnityExePath = "";
    public string BuildDir = "Build";
}

public enum BuildConfiguration
{
    Debug,
    Release,
}

public enum ErrorCode
{
    Ok                 = 0,
    GeneralError       = 1,
    UnityProjectLocked = 2,
}

public static partial class Constants
{
    public const string CONFIG_FILE_NAME = "local_dev.json.user";
}

public static partial class Funcs
{
    // Walks up from CWD / AppContext.BaseDirectory looking for a directory containing ModUnity/.
    // Returns that directory as the mod-repo root.
    public static string GetRepoRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "ModUnity"))) return dir.FullName;
                dir = dir.Parent;
            }
        }
        throw new InvalidOperationException("Could not find mod-repo root (no parent dir with ModUnity/).");
    }

    public static Config LoadConfig()
    {
        var path = Path.Combine(G.repoRoot, CONFIG_FILE_NAME);
        if (!File.Exists(path))
        {
            LogError($"Config file not found: {path}");
            return new Config();
        }
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            }) ?? new Config();
        }
        catch (Exception ex)
        {
            LogError($"Failed to parse {path}: {ex.Message}");
            return new Config();
        }
    }
}
