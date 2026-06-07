namespace InsanityWorldMod.Core
{
    public interface IInsanityWorldSystem
    {
        int Order { get; }
        void OnLoad();
    }
}
