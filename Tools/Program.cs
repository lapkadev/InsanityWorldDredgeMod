using InsanityWorldMod.Tools;

const string Usage =
    "Usage: dotnet run --project Tools -- <command>\n" +
    "Commands:\n" +
    "  bootstrap                              Fetch DREDGE DLLs from NuGet to ModUnity/Assets/Plugins/Dredge (skipped if Winch.dll already present).\n" +
    "  build-all                              Build all Unity projects (DLLs and Bundles).\n" +
    "  deploy                                 Build + deploy mod to DREDGE/Mods.\n" +
    "  release-zip                            Build + zip Release artifacts for GitHub.\n" +
    "  bump-patch | bump-minor | bump-major   Bump sem-ver in all VersionFiles from config.";

if (args.Length == 0)
{
    LogError(Usage);
    return 1;
}

G.repoRoot = GetRepoRoot();
G.cfg = LoadConfig();
G.buildConfig = ParseBuildConfig(args);
G.skipCleanBuildDir = ParseSkipCleanBuildDir(args);
G.skipUnityProject = ParseSkipUnityProject(args);

return args[0] switch
{
    "bootstrap"  => Bootstrap(),
    "build-all"  => BuildAll(),
    "deploy"     => DeployToDredge(),
    "release-zip" => ReleaseZip(),
    "bump-patch" => BumpVersion(componentIndex: 2),
    "bump-minor" => BumpVersion(componentIndex: 1),
    "bump-major" => BumpVersion(componentIndex: 0),
    _            => Fail($"Unknown command: {args[0]}"),
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

static bool ParseSkipCleanBuildDir(string[] args)
{
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i] == "--skip-clean-build-dir") return true;
    }
    return false;
}

static string ParseSkipUnityProject(string[] args)
{
    for (int i = 1; i < args.Length - 1; i++)
    {
        if (args[i] == "--skip-unity-project") return Path.GetFullPath(args[i + 1]).ToLowerInvariant();
    }
    return "";
}
