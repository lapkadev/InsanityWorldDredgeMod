namespace InsanityWorldMod.Tools;

internal static class RepoLocator
{
    public static string FindModRepoRoot()
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
}
