namespace InsW.Core
{
    public interface IModSystem
    {
        int Order { get; }
        void OnLoad();
    }
}
