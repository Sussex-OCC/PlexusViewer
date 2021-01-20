﻿namespace Sussex.Lhcra.Roci.Viewer.UI.Configurations
{
    public class ViewerAppSettingsConfiguration
    {
        public string ApplicationName { get; set; }

        public double SessionTimeout { get; set; }

        public string OrganisationAsId { get; set; }

        public ProxyEndPointsConfiguration ProxyEndpoints { get; set; }

        public DatabaseConnectionStrings DatabaseConnectionStrings { get; set; } = new DatabaseConnectionStrings();

        public KeyVaultConfiguration KeyVault { get; set; } = new KeyVaultConfiguration();
    }
}