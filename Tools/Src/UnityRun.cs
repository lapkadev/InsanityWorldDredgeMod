using System.Diagnostics;
using System.Text.Json.Nodes;

namespace InsanityWorldMod.Tools;

public static partial class Funcs
{
    public static int UnityRun(string projectPath, string method, JsonObject jArgs)
    {
        if (string.IsNullOrEmpty(G.cfg.UnityExePath) || !File.Exists(G.cfg.UnityExePath))
        {
            LogError($"UnityExePath not set or not found: '{G.cfg.UnityExePath}' (check {CONFIG_FILE_NAME})");
            return 1;
        }
        if (!Directory.Exists(projectPath))
        {
            LogError($"Unity project path does not exist: {projectPath}");
            return 1;
        }

        var args = jArgs.ToJsonString();

        var buildDir    = jArgs["BuildDir"]?.GetValue<string>() ?? "";
        var buildConfig = jArgs["BuildConfiguration"]?.GetValue<string>() ?? "Release";
        var tmpDir      = Path.Combine(buildDir, "bin", buildConfig, ".tmp");
        Directory.CreateDirectory(tmpDir);
        var argsFile = Path.Combine(tmpDir, "unity_args.json");
        File.WriteAllText(argsFile, args);

        LogInfo($"Unity:     {G.cfg.UnityExePath}");
        LogInfo($"Project:   {projectPath}");
        LogInfo($"Method:    {method}");
        LogInfo($"Args:      {args}");
        LogInfo($"Args file: {argsFile}");

        var psi = new ProcessStartInfo(G.cfg.UnityExePath) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        psi.ArgumentList.Add("-batchmode");
        psi.ArgumentList.Add("-nographics");
        psi.ArgumentList.Add("-quit");
        psi.ArgumentList.Add("-projectPath"); psi.ArgumentList.Add(projectPath);
        psi.ArgumentList.Add("-executeMethod"); psi.ArgumentList.Add(method);
        psi.ArgumentList.Add("-logFile"); psi.ArgumentList.Add("-");
        psi.ArgumentList.Add("-args"); psi.ArgumentList.Add(argsFile);

        const string LOCKED_MARKER = "Multiple Unity instances cannot open the same project";
        bool projectLocked = false;

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (s, e) =>
        {
            if (e.Data == null) return;
            Console.WriteLine(e.Data);
            if (e.Data.Contains(LOCKED_MARKER)) projectLocked = true;
        };
        proc.ErrorDataReceived += (s, e) =>
        {
            if (e.Data == null) return;
            Console.Error.WriteLine(e.Data);
            if (e.Data.Contains(LOCKED_MARKER)) projectLocked = true;
        };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();

        if (projectLocked)
        {
            LogError($"Unity project is already open in another Editor instance: {projectPath}. Close it and retry.");
            return (int)ErrorCode.UnityProjectLocked;
        }

        LogInfo($"Unity exit code: {proc.ExitCode}");
        return proc.ExitCode;
    }
}
