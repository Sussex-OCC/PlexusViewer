using Sussex.Lhcra.Roci.Viewer.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI
{
    public class CertificateHttpClientHandler : HttpClientHandler
    {       

        public CertificateHttpClientHandler(IAppSecretsProvider appSecretsProvider)
        {

            var certificate = appSecretsProvider.GetCertificate("GatewayClientCertificate").GetAwaiter().GetResult();
            ClientCertificateOptions = ClientCertificateOption.Manual;
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            ClientCertificates.Add(certificate);
        }
    }
}
