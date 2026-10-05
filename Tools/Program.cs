using InsW.Tools;

const string Usage =
    "Usage: dotnet run --project Tools -- <command>\n" +
    "Commands:\n" +
    "  bootstrap    Fetch DREDGE DLLs from NuGet to ModUnity/Assets/Plugins/Dredge and the Yarn package.\n" +
    "  build-all    Build the Unity project and the mod loader.";

if (args.Length == 0)
{
    LogError(Usage);
    return 1;
}

G.repoRoot = GetRepoRoot();
G.cfg = LoadConfig();
G.buildConfig = ParseBuildConfig(args);

return args[0] switch
{
    "bootstrap" => Bootstrap(),
    "build-all" => BuildAll(),
    _           => Fail($"Unknown command: {args[0]}"),
};

static int Fail(string msg)
{
    LogError(msg);
    LogError(Usage);
    return 1;
}

static BuildConfiguration ParseBuildConfig(string[] args)
{
    if (args.Length > 1 && !args[1].StartsWith("--"))
    {
        if (Enum.TryParse<BuildConfiguration>(args[1], ignoreCase: true, out var c)) return c;
    }
    return BuildConfiguration.Release;
}
