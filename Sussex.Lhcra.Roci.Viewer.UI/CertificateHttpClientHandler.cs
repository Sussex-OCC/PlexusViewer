using Microsoft.Extensions.Options;
using Sussex.Lhcra.Roci.Viewer.Services.Configurations;
using Sussex.Lhcra.Roci.Viewer.Services.Core;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI
{
    public class CertificateHttpClientHandler : DelegatingHandler
    {
        private readonly ICertificateProvider _appSecretsProvider;
        private readonly ClientCertificateConfig _clientCerConfig;

        public CertificateHttpClientHandler(ICertificateProvider appSecretsProvider, IOptions<ClientCertificateConfig> clientCertOptions)
        {
            _appSecretsProvider = appSecretsProvider;
            _clientCerConfig = clientCertOptions.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var certificate = await _appSecretsProvider.GetCertificate(_clientCerConfig.KeyVaultCertificateName);
            request.Headers.Add("X-ClientCert", Convert.ToBase64String(certificate.GetRawCertData()));
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
