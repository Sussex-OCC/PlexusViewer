using Sussex.Lhcra.Roci.Viewer.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI
{
    public class CertificateHttpClientHandler : DelegatingHandler
    {
        private readonly IAppSecretsProvider _appSecretsProvider;

        public CertificateHttpClientHandler(IAppSecretsProvider appSecretsProvider)
        {
            _appSecretsProvider = appSecretsProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var certificate = await _appSecretsProvider.GetCertificate("GatewayClientCertificate");
            request.Headers.Add("X-ARR-ClientCert", certificate.GetRawCertDataString());
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
