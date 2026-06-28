using static InsanityWorldMod.Core.DredgeHooks;
using static InsanityWorldMod.Core.Funcs;

namespace InsanityWorldMod.Core
{
    public class LoadDataSystem : IInsanityWorldSystem
    {
        public int Order => 2;

        public void OnLoad()
        {
            foreach (var bundle in GetAllBundles())
                LoadAll(bundle);
        }
    }
}
