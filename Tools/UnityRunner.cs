using System.Diagnostics;
using System.Text.Json.Nodes;

namespace InsanityWorldMod.Tools;

public static class UnityRunner
{
    public static int RunBatch(string executeMethod, bool needsDredgeModsFolder)
    {
        string repoRoot = RepoLocator.FindModRepoRoot();
        string projectPath = Path.Combine(repoRoot, "ModUnity");

        if (!File.Exists(Path.Combine(projectPath, "Assets", "Plugins", "Dredge", "Winch.dll")))
        {
            Console.WriteLine("DREDGE DLLs missing - running bootstrap first...");
            int boot = Bootstrap.Run();
            if (boot != 0) return boot;
            Console.WriteLine();
        }

        var cfg = LocalConfig.Load(repoRoot);

        string? unityExe = ResolveUnityExe(repoRoot, cfg, projectPath);
        if (unityExe == null) return 1;

        if (needsDredgeModsFolder && ResolveDredgeModsFolder(repoRoot, cfg) == null) return 1;

        Console.WriteLine($"Unity:         {unityExe}");
        Console.WriteLine($"Project:       {projectPath}");
        Console.WriteLine($"Execute:       {executeMethod}");
        Console.WriteLine();

        var psi = new ProcessStartInfo(unityExe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-batchmode");
        psi.ArgumentList.Add("-nographics");
        psi.ArgumentList.Add("-quit");
        psi.ArgumentList.Add("-projectPath"); psi.ArgumentList.Add(projectPath);
        psi.ArgumentList.Add("-executeMethod"); psi.ArgumentList.Add(executeMethod);
        psi.ArgumentList.Add("-logFile"); psi.ArgumentList.Add("-");

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        Console.WriteLine();
        Console.WriteLine($"Unity exited with code: {proc.ExitCode}");
        return proc.ExitCode;
    }

    private static string? ResolveUnityExe(string repoRoot, JsonObject cfg, string projectPath)
    {
        string? configured = LocalConfig.GetString(cfg, "unityExePath");
        if (!string.IsNullOrEmpty(configured) && File.Exists(configured)) return configured;

        string version = ReadUnityVersion(projectPath);
        string hubExe = $@"C:\Program Files\Unity\Hub\Editor\{version}\Editor\Unity.exe";
        if (File.Exists(hubExe)) return hubExe;

        Console.WriteLine($"Unity {version} not found at default location:");
        Console.WriteLine($"  {hubExe}");
        Console.WriteLine();
        return PromptForPath(repoRoot, cfg, "unityExePath",
            "Enter full path to Unity.exe", File.Exists);
    }

    private static string? ResolveDredgeModsFolder(string repoRoot, JsonObject cfg)
    {
        string? configured = LocalConfig.GetString(cfg, "dredgeModsFolder");
        if (!string.IsNullOrEmpty(configured) && Directory.Exists(configured)) return configured;

        return PromptForPath(repoRoot, cfg, "dredgeModsFolder",
            "Enter DREDGE Mods folder (e.g. C:/Games/DREDGE/Mods)", Directory.Exists);
    }

    private static string? PromptForPath(string repoRoot, JsonObject cfg, string key, string prompt, Func<string, bool> validate)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            string? input = Console.ReadLine()?.Trim().Trim('"');
            if (string.IsNullOrEmpty(input))
            {
                Console.Error.WriteLine("Aborted.");
                return null;
            }
            if (validate(input))
            {
                LocalConfig.SetString(cfg, key, input);
                LocalConfig.Save(repoRoot, cfg);
                Console.WriteLine($"Saved {key} = {input} to local_dev.json.user");
                return input;
            }
            Console.Error.WriteLine("Invalid path, try again (empty line to abort).");
        }
    }

    private static string ReadUnityVersion(string projectPath)
    {
        string versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        foreach (var line in File.ReadAllLines(versionFile))
        {
            if (line.StartsWith("m_EditorVersion:"))
                return line.Substring("m_EditorVersion:".Length).Trim();
        }
        throw new InvalidOperationException($"m_EditorVersion not found in {versionFile}");
    }
}
