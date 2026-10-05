using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace InsW.Core
{
    public static partial class G
    {
        public static List<Type>                 ModSystemTypes = new List<Type>();
        public static List<IModSystem> ModSystems     = new List<IModSystem>();
    }

    public static partial class Funcs
    {
        public static bool IsModSystemType(Type type)
        {
            return typeof(IModSystem).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface;
        }

        public static bool IsNewModSystemType(Type type)
        {
            return IsModSystemType(type) && !G.ModSystemTypes.Contains(type);
        }

        public static Type[] GetModAssemblyTypes(string assemblyName)
        {
            try
            {
                return Assembly.LoadFrom(Path.Combine(G.ModDir, assemblyName)).GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Warn($"ModSystems: some types of {assemblyName} are not readable: {ex.LoaderExceptions.FirstOrDefault()?.Message}");
                return ex.Types.Where(type => type != null).ToArray();
            }
            catch (Exception ex)
            {
                Log.Error($"ModSystems: failed to load {assemblyName}: {ex.Message}");
                return new Type[0];
            }
        }

        public static void FindModSystemTypes()
        {
            G.ModSystemTypes.Clear();
            if (G.ModFiles?.Assemblies == null)
            {
                Log.Error("ModSystems: mod files list is not loaded, no systems to find");
                return;
            }

            foreach (var assemblyName in G.ModFiles.Assemblies)
                G.ModSystemTypes.AddRange(GetModAssemblyTypes(assemblyName).Where(IsNewModSystemType));
        }

        public static void CreateModSystems()
        {
            G.ModSystems.Clear();
            foreach (var type in G.ModSystemTypes)
            {
                try
                {
                    G.ModSystems.Add((IModSystem)Activator.CreateInstance(type));
                }
                catch (Exception ex)
                {
                    Log.Error($"ModSystems: failed to create {type.FullName}: {(ex.InnerException ?? ex).Message}");
                }
            }
        }

        public static void RunModSystems()
        {
            var loaded = new List<string>();
            foreach (var system in G.ModSystems.OrderBy(s => s.Order).ToList())
            {
                try
                {
                    system.OnLoad();
                    loaded.Add($"{system.GetType().Name}({system.Order})");
                }
                catch (Exception ex)
                {
                    Log.Error($"ModSystems: {system.GetType().FullName}.OnLoad failed: {ex}");
                }
            }

            Log.Info($"ModSystems: mod {ModVersion.Current}, {loaded.Count} of {G.ModSystems.Count} systems loaded");
            DevLog.Info($"ModSystems: loaded in order: {string.Join(", ", loaded)}");
        }
    }
}
