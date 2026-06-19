using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using InsanityWorldMod.Core;

namespace InsanityWorldMod
{
	public class Loader
	{
		/// <summary>
		/// Run by Winch as a Preload step, before asset bundles are loaded
		/// </summary>
		public static void Preload()
		{
			var modDir = Path.GetDirectoryName(typeof(Loader).Assembly.Location);

			foreach (var path in Directory.GetFiles(modDir, "*.dll"))
			{
				try { Assembly.LoadFrom(path); }
				catch (Exception ex)
				{
					Console.Error.WriteLine($"[InsanityWorld] Preload failed to load {Path.GetFileName(path)}: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// This method is run by Winch to initialize your mod
		/// </summary>
		public static void Initialize()
		{
			var modDir = Path.GetDirectoryName(typeof(Loader).Assembly.Location);
			var systems = new List<(IInsanityWorldSystem system, string fileName)>();

			foreach (var path in Directory.GetFiles(modDir, "*.dll"))
			{
				Assembly asm;
				try { asm = Assembly.LoadFrom(path); }
				catch (Exception ex)
				{
					Console.Error.WriteLine($"[InsanityWorld] Failed to load assembly {Path.GetFileName(path)}: {ex.Message}");
					continue;
				}

				Type[] types;
				try { types = asm.GetTypes(); }
				catch (ReflectionTypeLoadException) { continue; }

				foreach (var t in types)
				{
					if (!typeof(IInsanityWorldSystem).IsAssignableFrom(t) || t.IsAbstract || t.IsInterface) continue;
					try
					{
						var instance = (IInsanityWorldSystem)Activator.CreateInstance(t);
						systems.Add((instance, Path.GetFileName(path)));
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine($"[InsanityWorld] Failed to instantiate {t.FullName} from {Path.GetFileName(path)}: {ex.Message}");
					}
				}
			}

			foreach (var (system, fileName) in systems.OrderBy(s => s.system.Order))
			{
				try
				{
					Console.WriteLine($"[InsanityWorld] Loading {system.GetType().FullName} (Order={system.Order}, from {fileName})");
					system.OnLoad();
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"[InsanityWorld] {system.GetType().FullName}.OnLoad failed: {ex}");
				}
			}
		}
	}
}
