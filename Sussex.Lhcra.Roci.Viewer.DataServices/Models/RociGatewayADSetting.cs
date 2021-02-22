using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Roci.Viewer.Domain;

namespace Sussex.Lhcra.Roci.Viewer.DataServices.Models
{
    public class RociGatewayADSetting : IAzureADSettings
    {
        public string[] UserScope { get; set; }

        public string SystemToSystemScope { get; set; }
    }
}
