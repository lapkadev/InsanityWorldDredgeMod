using System.Reflection;

[assembly: AssemblyVersion(InsanityWorldMod.Core.ModVersion.Current)]
[assembly: AssemblyFileVersion(InsanityWorldMod.Core.ModVersion.Current)]

namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Single source of truth for the mod's version
    /// Must be kept in sync with the mod-repo's mod_meta.json "Version" field.
    ///
    /// Both files are bumped together by the InsanityWorld / Bump Patch/Minor/Major
    /// menu items in BuildArtifacts.cs - do not edit manually unless you know why.
    /// </summary>
    public static class ModVersion
    {
        public const string Current = "0.2.0";
    }
}
