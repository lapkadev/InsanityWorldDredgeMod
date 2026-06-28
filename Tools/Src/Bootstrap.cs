using System.Diagnostics;
using System.Security.Cryptography;

namespace InsanityWorldMod.Tools;

public static partial class Constants
{
    public const string BOOTSTRAP_PLUGINS_REL_DIR = "ModUnity/Assets/Plugins/Dredge";
    public const string BOOTSTRAP_YARN_REL_DIR    = "ModUnity/Assets/Plugins/Yarn";
    public const string BOOTSTRAP_YARN_GIT_URL    = "https://github.com/YarnSpinnerTool/YarnSpinner-Unity.git";
    public const string BOOTSTRAP_YARN_GIT_TAG    = "v2.2.0";

    public const string BOOTSTRAP_MIRROR_REL_DIR  = "ModUnity/Assets/Plugins/Mirror";
    public const string BOOTSTRAP_MIRROR_GIT_URL  = "https://github.com/MirrorNetworking/Mirror.git";
    public const string BOOTSTRAP_MIRROR_GIT_TAG  = "v96.10.3";
}

public static partial class Funcs
{
    public static int Bootstrap()
    {
        int res = BootstrapDredgeAndWinch();
        if (res != 0) return res;

        res = BootstrapYarn();
        if (res != 0) return res;

        res = BootstrapMirror();
        if (res != 0) return res;

        return 0;
    }

    private static readonly (string Name, string Package, string Version, string Subpath, string Sha256)[] BootstrapDlls = new[]
    {
        ("Assembly-CSharp-firstpass.dll",                       "DredgeGameLibs", "1.5.3", "lib",       "B8E700DDF6BDAB33DEC44AE675BBA17270FCD383C32D8CE12C4D11AC6F62A0A3"),
        ("Assembly-CSharp.dll",                                 "DredgeGameLibs", "1.5.3", "lib",       "3E9B0E429E36E3BDB3346D990AE5C5A502A9AC3C653757E137D22FB6D22CA674"),
        ("Sirenix.OdinInspector.Attributes.dll",                "DredgeGameLibs", "1.5.3", "lib",       "E408CC38E6F6B72B0D6E0D4735D15E41F69B2B97A8765A4CD3CCDC6A69D257DD"),
        ("Sirenix.OdinInspector.CompatibilityLayer.dll",        "DredgeGameLibs", "1.5.3", "lib",       "E62292F5AA94A40E73CD97491F85C82C0E22D15F2A072399FDCC709A441FBB41"),
        ("Sirenix.OdinInspector.Modules.UnityLocalization.dll", "DredgeGameLibs", "1.5.3", "lib",       "F16F764867834CA80911D8B1D5A9781F940EFFDB4EEAC3DDF34781A9EF964C26"),
        ("Sirenix.Serialization.Config.dll",                    "DredgeGameLibs", "1.5.3", "lib",       "4A4BCE970AC2D876034706C9961F8017B43CE8E41D7231FFA00D21FC352A59A3"),
        ("Sirenix.Serialization.dll",                           "DredgeGameLibs", "1.5.3", "lib",       "5D58F6DE23E1BCA3237F2B7D6F8DE5DE66718D510B449FF4B422589A929EB4E8"),
        ("Sirenix.Utilities.dll",                               "DredgeGameLibs", "1.5.3", "lib",       "FA44E5604F06616B70522D08EBD73457BD1DAE189C79867E5D7402C2A7BD8D87"),
        ("Winch.dll",                                           "Winch",          "0.6.2", "lib/net48", "21C10E4D6878345BB2CE55087B42DE86B243390FDC221D45E72A30E381E8A5AC"),
        ("WinchCommon.dll",                                     "Winch",          "0.6.2", "lib/net48", "3AAB4C11ACACF516E0BC3A321438F1970469E74381094471B16314D3B08D768A"),
    };

    public static int BootstrapDredgeAndWinch()
    {
        string pluginsDir = Path.Combine(G.repoRoot, BOOTSTRAP_PLUGINS_REL_DIR.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(Path.Combine(pluginsDir, "Winch.dll")))
        {
            LogInfo($"Bootstrap: DREDGE DLLs already in {pluginsDir} - skipping.");
            return 0;
        }

        string nugetCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        Directory.CreateDirectory(pluginsDir);

        LogInfo($"Mod repo root: {G.repoRoot}");
        LogInfo($"NuGet cache:   {nugetCache}");
        LogInfo($"Target dir:    {pluginsDir}");

        int verified = 0, failed = 0;
        foreach (var d in BootstrapDlls)
        {
            string src = Path.Combine(nugetCache, d.Package.ToLowerInvariant(), d.Version,
                d.Subpath.Replace('/', Path.DirectorySeparatorChar), d.Name);
            if (!File.Exists(src))
            {
                LogError($"MISSING source: {src}");
                failed++;
                continue;
            }

            string dst = Path.Combine(pluginsDir, d.Name);
            File.Copy(src, dst, overwrite: true);

            string actualHash = BootstrapSha256(dst);
            if (!string.Equals(actualHash, d.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                LogError($"HASH MISMATCH {d.Name}: expected {d.Sha256}, got {actualHash}");
                failed++;
            }
            else
            {
                LogInfo($"OK  {d.Name}");
                verified++;
            }
        }

        LogInfo($"Done. Verified {verified}/{BootstrapDlls.Length}, failed {failed}.");
        return failed > 0 ? 1 : 0;
    }

    public static int BootstrapYarn()
    {
        string yarnDir = Path.Combine(G.repoRoot, BOOTSTRAP_YARN_REL_DIR.Replace('/', Path.DirectorySeparatorChar));
        string sentinelAsmdef = Path.Combine(yarnDir, "Runtime", "YarnSpinner.Unity.asmdef");

        if (File.Exists(sentinelAsmdef))
        {
            LogInfo($"Bootstrap: Yarn package already in {yarnDir} - skipping.");
            return 0;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"yarnspinner-bootstrap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            LogInfo($"Bootstrap: cloning {BOOTSTRAP_YARN_GIT_URL} tag {BOOTSTRAP_YARN_GIT_TAG} -> {tempDir}");
            int gitExit = RunProcess("git", $"clone --depth 1 --branch {BOOTSTRAP_YARN_GIT_TAG} \"{BOOTSTRAP_YARN_GIT_URL}\" \"{tempDir}\"");
            if (gitExit != 0)
            {
                LogError($"git clone failed (exit {gitExit}). Is 'git' installed and on PATH?");
                return 1;
            }

            Directory.CreateDirectory(yarnDir);
            CopyYarnFiltered(tempDir, yarnDir);

            LogInfo($"Bootstrap: Yarn package installed at {yarnDir}");
            return 0;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { DeleteDirectoryForced(tempDir); }
                catch (Exception ex) { LogError($"Failed to cleanup temp dir {tempDir}: {ex.Message}"); }
            }
        }
    }

    private static void DeleteDirectoryForced(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, recursive: true);
    }

    private static void CopyYarnFiltered(string srcRoot, string dstRoot)
    {
        var excludedDirs = new[] { ".git", "Tests" };
        // Conflicts with NuGetForUnity's copy of the same DLL - System.Reflection.TypeExtensions.dll (HarmonyX/Mono.Cecil transitive dep)
        var excludedFiles = new[] { "System.Reflection.TypeExtensions.dll", "System.Reflection.TypeExtensions.dll.meta", "Tests.meta" };

        int copiedFiles = 0;
        foreach (var srcFile in Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(srcRoot, srcFile);
            string[] segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(s => excludedDirs.Contains(s, StringComparer.OrdinalIgnoreCase))) continue;
            if (excludedFiles.Contains(Path.GetFileName(srcFile), StringComparer.OrdinalIgnoreCase)) continue;

            string dstFile = Path.Combine(dstRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
            File.Copy(srcFile, dstFile, overwrite: true);
            copiedFiles++;
        }
        LogInfo($"Bootstrap: copied {copiedFiles} file(s) from Yarn package (excluded: Tests/, .git/, System.Reflection.TypeExtensions.dll)");
    }

    public static int BootstrapMirror()
    {
        string mirrorDir = Path.Combine(G.repoRoot, BOOTSTRAP_MIRROR_REL_DIR.Replace('/', Path.DirectorySeparatorChar));

        if (Directory.Exists(mirrorDir) && Directory.EnumerateFileSystemEntries(mirrorDir).Any())
        {
            LogInfo($"Bootstrap: Mirror package already in {mirrorDir} - skipping.");
            return 0;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"mirror-bootstrap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            LogInfo($"Bootstrap: cloning {BOOTSTRAP_MIRROR_GIT_URL} tag {BOOTSTRAP_MIRROR_GIT_TAG} -> {tempDir}");
            int gitExit = RunProcess("git", $"clone --depth 1 --branch {BOOTSTRAP_MIRROR_GIT_TAG} \"{BOOTSTRAP_MIRROR_GIT_URL}\" \"{tempDir}\"");
            if (gitExit != 0)
            {
                LogError($"git clone failed (exit {gitExit}). Is 'git' installed and on PATH?");
                return 1;
            }

            string srcMirror = Path.Combine(tempDir, "Assets", "Mirror");
            if (!Directory.Exists(srcMirror))
            {
                LogError($"Mirror sources not found at {srcMirror} - repository layout changed?");
                return 1;
            }

            Directory.CreateDirectory(mirrorDir);
            CopyTreeFiltered(srcMirror, mirrorDir,
                new[] { ".git", "Tests", "Examples", "PredictedRigidbody", "Encryption" },
                new[] { "Tests.meta", "Examples.meta", "PredictedRigidbody.meta", "Encryption.meta" });

            LogInfo($"Bootstrap: Mirror package installed at {mirrorDir}");
            return 0;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { DeleteDirectoryForced(tempDir); }
                catch (Exception ex) { LogError($"Failed to cleanup temp dir {tempDir}: {ex.Message}"); }
            }
        }
    }

    private static void CopyTreeFiltered(string srcRoot, string dstRoot, string[] excludedDirs, string[] excludedFiles)
    {
        int copiedFiles = 0;
        foreach (var srcFile in Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(srcRoot, srcFile);
            string[] segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(s => excludedDirs.Contains(s, StringComparer.OrdinalIgnoreCase))) continue;
            if (excludedFiles.Contains(Path.GetFileName(srcFile), StringComparer.OrdinalIgnoreCase)) continue;

            string dstFile = Path.Combine(dstRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
            File.Copy(srcFile, dstFile, overwrite: true);
            copiedFiles++;
        }
        LogInfo($"Bootstrap: copied {copiedFiles} file(s) into {dstRoot}");
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) LogInfo(e.Data); };
        proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) LogError(e.Data); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.WaitForExit();
        return proc.ExitCode;
    }

    private static string BootstrapSha256(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
