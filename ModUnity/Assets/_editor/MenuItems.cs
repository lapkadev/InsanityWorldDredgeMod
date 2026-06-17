using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;
using static InsanityWorldMod.Editor.Funcs;

namespace InsanityWorldMod.Editor
{
    public static class MenuItems
    {
        // ==================== Bump version ====================

        [MenuItem("InsanityWorld/Bump Patch (x.y.Z)", false, 1)]
        public static void BumpPatch() => RunBump("bump-patch");

        [MenuItem("InsanityWorld/Bump Minor (x.Y.0)", false, 2)]
        public static void BumpMinor() => RunBump("bump-minor");

        [MenuItem("InsanityWorld/Bump Major (X.0.0)", false, 3)]
        public static void BumpMajor() => RunBump("bump-major");

        // ==================== Build All DLLs and Bundles ====================

        [MenuItem("InsanityWorld/Build All DLLs and Bundles (Debug)", false, 50)]
        public static void BuildAllDebug() => RunFlow("build-all", "Debug");

        [MenuItem("InsanityWorld/Build All DLLs and Bundles (Release)", false, 51)]
        public static void BuildAllRelease() => RunFlow("build-all", "Release");

        // ==================== Build All DLLs and Bundles + Deploy to DREDGE ====================

        [MenuItem("InsanityWorld/Build All DLLs and Bundles + Deploy to DREDGE (Debug)", false, 100)]
        public static void DeployDebug() => RunFlow("deploy", "Debug");

        [MenuItem("InsanityWorld/Build All DLLs and Bundles + Deploy to DREDGE (Release)", false, 101)]
        public static void DeployRelease() => RunFlow("deploy", "Release");

        // ==================== Build All DLLs and Bundles + zip-archive for GitHub ====================

        [MenuItem("InsanityWorld/Build All DLLs and Bundles + zip-archive for GitHub (Release)", false, 200)]
        public static void ReleaseZip() => RunFlow("release-zip", "Release");

        // ==================== Internal: Build flow (in-process BuildAll + spawn Tools) ====================

        private static void RunFlow(string toolsCommand, string buildConfig)
        {
            var unityProjectPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var repoRoot = Path.GetFullPath(Path.Combine(unityProjectPath, ".."));
            var buildDir = Path.Combine(repoRoot, "Build");

            Debug.Log($"[InsanityWorld] MenuItems: starting '{toolsCommand} {buildConfig}' (current Unity project = {unityProjectPath})");

            var args = new BuildAllArgs
            {
                BuildDir = buildDir,
                BuildConfiguration = buildConfig,
            };
            if (!CleanBuildDir(args))
            {
                Debug.LogError("[InsanityWorld] MenuItems: in-process CleanBuildDir FAILED. Aborting.");
                return;
            }
            if (!BuildAll(args))
            {
                Debug.LogError("[InsanityWorld] MenuItems: in-process BuildAll FAILED. Aborting.");
                return;
            }

            var psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add("Tools");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add(toolsCommand);
            psi.ArgumentList.Add(buildConfig);
            psi.ArgumentList.Add("--skip-clean-build-dir");
            psi.ArgumentList.Add("--skip-unity-project");
            psi.ArgumentList.Add(unityProjectPath);

            Debug.Log($"[InsanityWorld] MenuItems: spawning Tools: dotnet run --project Tools -- {toolsCommand} {buildConfig} --skip-clean-build-dir --skip-unity-project \"{unityProjectPath}\"");

            RunDotnetProcess(psi, $"{toolsCommand} {buildConfig}");
        }

        // ==================== Internal: Bump flow (spawn Tools only) ====================

        private static void RunBump(string toolsCommand)
        {
            var repoRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ".."));

            var psi = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add("--project");
            psi.ArgumentList.Add("Tools");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add(toolsCommand);

            Debug.Log($"[InsanityWorld] MenuItems: spawning Tools: dotnet run --project Tools -- {toolsCommand}");

            RunDotnetProcess(psi, toolsCommand);
        }

        private static void RunDotnetProcess(ProcessStartInfo psi, string label)
        {
            using var proc = Process.Start(psi);
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(stdout)) Debug.Log($"[InsanityWorld][Tools stdout]\n{stdout}");
            if (!string.IsNullOrEmpty(stderr)) Debug.LogError($"[InsanityWorld][Tools stderr]\n{stderr}");

            if (proc.ExitCode != 0)
            {
                Debug.LogError($"[InsanityWorld] MenuItems: Tools '{label}' FAILED (exit code {proc.ExitCode}).");
            }
            else
            {
                Debug.Log($"[InsanityWorld] MenuItems: '{label}' DONE.");
                AssetDatabase.Refresh();
            }
        }
    }
}
