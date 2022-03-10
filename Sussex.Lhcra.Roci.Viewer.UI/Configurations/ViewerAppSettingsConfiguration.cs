namespace Sussex.Lhcra.Plexus.Viewer.UI.Configurations
{
    public class ViewerAppSettingsConfiguration
    {
        public string ApplicationName { get; set; }

        public double SessionTimeout { get; set; }

        public string OrganisationAsId { get; set; }

        public bool UseLocalCertificate { get; set; }
        public bool FetchMentalHealthData { get; set; }

        public ProxyEndPointsConfiguration ProxyEndpoints { get; set; }

        public DatabaseConnectionStrings DatabaseConnectionStrings { get; set; } = new DatabaseConnectionStrings();

        public KeyVaultConfiguration KeyVault { get; set; } = new KeyVaultConfiguration();
    }
}
