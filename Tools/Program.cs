using InsanityWorldMod.Tools;

const string Usage = "Usage: dotnet run --project Tools -- <bootstrap|deploy|archive|bump-patch|bump-minor|bump-major>";

if (args.Length == 0)
{
    Console.Error.WriteLine(Usage);
    return 1;
}

return args[0] switch
{
    "bootstrap"  => Bootstrap.Run(),
    "deploy"     => UnityRunner.RunBatch("InsanityWorldMod.EditorTools.BuildArtifacts.BuildAllReleaseDeploy",  needsDredgeModsFolder: true),
    "archive"    => UnityRunner.RunBatch("InsanityWorldMod.EditorTools.BuildArtifacts.BuildReleaseAndPackage", needsDredgeModsFolder: false),
    "bump-patch" => BumpVersion.Run(componentIndex: 2),
    "bump-minor" => BumpVersion.Run(componentIndex: 1),
    "bump-major" => BumpVersion.Run(componentIndex: 0),
    _            => Fail($"Unknown command: {args[0]}"),
};

static int Fail(string msg)
{
    Console.Error.WriteLine(msg);
    Console.Error.WriteLine(Usage);
    return 1;
}
