namespace InsanityWorldMod.Core
{
    public class EosCredentials
    {
        public string ProductName;
        public string ProductVersion;
        public string ProductId;
        public string SandboxId;
        public string DeploymentId;
        public string ClientId;
        public string ClientSecret;

        public bool IsComplete =>
            !string.IsNullOrEmpty(ProductId)
            && !string.IsNullOrEmpty(SandboxId)
            && !string.IsNullOrEmpty(DeploymentId)
            && !string.IsNullOrEmpty(ClientId)
            && !string.IsNullOrEmpty(ClientSecret);
    }
}
