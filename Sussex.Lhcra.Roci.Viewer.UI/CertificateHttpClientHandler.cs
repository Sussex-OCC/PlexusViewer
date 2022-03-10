﻿using Microsoft.Extensions.Options;
using Sussex.Lhcra.Plexus.Viewer.Services;
using Sussex.Lhcra.Plexus.Viewer.Services.Configurations;
using Sussex.Lhcra.Plexus.Viewer.Services.Core;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.UI
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
            X509Certificate2 certificate = null;

            try
            {
                certificate = await _appSecretsProvider.GetCertificate(_clientCerConfig.KeyVaultCertificateName);
            }
            catch
            {
                throw new InvalidCertificateException("Invalid or wrong certificate name.");
            }

            request.Headers.Add("X-ClientCert", Convert.ToBase64String(certificate.GetRawCertData()));

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
