using System.IO;
using InsW.Core;
using static InsW.Core.Funcs;

namespace InsW
{
	public class Loader
	{
		/// <summary>
		/// Run by Winch as a Preload step, before asset bundles are loaded
		/// </summary>
		public static void Preload()
		{
			G.ModDir = Path.GetDirectoryName(typeof(Loader).Assembly.Location);
			LoadModFilesJson();
			LoadModAssemblies();
		}

		/// <summary>
		/// This method is run by Winch to initialize your mod
		/// </summary>
		public static void Initialize()
		{
			FindModSystemTypes();
			CreateModSystems();
			RunModSystems();
		}
	}
}
