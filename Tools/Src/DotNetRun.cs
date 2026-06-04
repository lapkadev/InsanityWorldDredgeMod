using System.Diagnostics;

namespace InsanityWorldMod.Tools;

public static partial class Funcs
{
    public static int DotNetRun(string projectPath)
    {
        var fullPath = Path.IsPathRooted(projectPath)
            ? projectPath
            : Path.GetFullPath(Path.Combine(G.repoRoot, projectPath));

        if (!File.Exists(fullPath))
        {
            LogError($".csproj not found: {fullPath}");
            return 1;
        }

        LogInfo($"dotnet build: {fullPath}");
        LogInfo($"Config:       {G.buildConfig}");

        var psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add(fullPath);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(G.buildConfig.ToString());

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        LogInfo($"dotnet exit code: {proc.ExitCode}");
        return proc.ExitCode;
    }
}
