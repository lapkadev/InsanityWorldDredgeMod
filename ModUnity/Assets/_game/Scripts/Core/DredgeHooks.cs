using System;

namespace InsanityWorldMod.Core
{
    /// <summary>
    /// Delegates for DREDGE / Winch APIs that Core cannot reference at compile time.
    /// Core.asmdef intentionally does NOT reference Winch - keeps the DLL boundary clean
    /// (Core = pure gameplay, Api = mod-loader glue).
    /// Api layer MUST wire these at <c>EntrySystem.OnLoad()</c> before any Core code runs.
    ///
    /// Pattern: dependency inversion - Core declares the contract, Api supplies the implementation.
    /// No defaults: if Api forgets to wire a hook, calling it throws NullReferenceException -
    /// surfacing the missing wiring loudly rather than silently no-op'ing.
    /// </summary>
    public static class DredgeHooks
    {
        /// <summary>
        /// Resolves a Dock instance by its string id.
        /// </summary>
        public static Func<string, Dock> FindDockById { get; set; }

        /// <summary>
        /// Returns the base path of the currently-loaded mod folder (used for debug save location).
        /// </summary>
        public static Func<string> GetModBasePath { get; set; }
    }
}
