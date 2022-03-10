using Sussex.Lhcra.Common.AzureADServices.Interfaces;

namespace Sussex.Lhcra.Plexus.Viewer.DataServices.Models
{
    public class PlexusGatewayAdSetting : IAzureADSettings
    {
        public string[] UserScope { get; set; }

        public string SystemToSystemScope { get; set; }
    }
}
