using System.Security.Cryptography;

namespace InsanityWorldMod.Tools;

public static class Bootstrap
{
    private const string PluginsRelDir = "ModUnity/Assets/Plugins/Dredge";

    private static readonly (string Name, string Package, string Version, string Subpath, string Sha256)[] Dlls = new[]
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

    public static int Run()
    {
        string modRepoRoot = RepoLocator.FindModRepoRoot();
        string nugetCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        string pluginsDir = Path.Combine(modRepoRoot, PluginsRelDir.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(pluginsDir);

        Console.WriteLine($"Mod repo root: {modRepoRoot}");
        Console.WriteLine($"NuGet cache:   {nugetCache}");
        Console.WriteLine($"Target dir:    {pluginsDir}");
        Console.WriteLine();

        int verified = 0, failed = 0;
        foreach (var d in Dlls)
        {
            string src = Path.Combine(nugetCache, d.Package.ToLowerInvariant(), d.Version,
                d.Subpath.Replace('/', Path.DirectorySeparatorChar), d.Name);
            if (!File.Exists(src))
            {
                Console.Error.WriteLine($"MISSING source: {src}");
                failed++;
                continue;
            }

            string dst = Path.Combine(pluginsDir, d.Name);
            File.Copy(src, dst, overwrite: true);

            string actualHash = ComputeSha256(dst);
            if (!string.Equals(actualHash, d.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"HASH MISMATCH {d.Name}: expected {d.Sha256}, got {actualHash}");
                failed++;
            }
            else
            {
                Console.WriteLine($"OK  {d.Name}");
                verified++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done. Verified {verified}/{Dlls.Length}, failed {failed}.");
        return failed > 0 ? 1 : 0;
    }

    private static string ComputeSha256(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
