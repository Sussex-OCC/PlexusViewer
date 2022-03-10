using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.Services
{
    public class GraphAuthenticationProvider : IAuthenticationProvider
    {
        public const string GRAPH_URI = "https://graph.microsoft.com/";
        private string _tenantId { get; set; }
        private string _clientId { get; set; }
        private string _clientSecret { get; set; }



        public GraphAuthenticationProvider(IConfiguration configuration)
        {
            _tenantId = configuration.GetValue<string>("TenantId");
            _clientId = configuration.GetValue<string>("ClientId");
            _clientSecret = configuration.GetValue<string>("ClientSecret");
        }
        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }
    }
}
