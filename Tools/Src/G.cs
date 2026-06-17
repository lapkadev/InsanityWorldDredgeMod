namespace InsanityWorldMod.Tools;

public static partial class G
{
    public static string repoRoot = null!;
    public static Config cfg = null!;
    public static BuildConfiguration buildConfig = BuildConfiguration.Release;
    public static bool skipCleanBuildDir = false;
    public static string skipUnityProject = "";
}
