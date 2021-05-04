using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.Services.Core
{
    public interface ICertificateProvider
    { 
        Task<X509Certificate2> GetCertificate(string certificateName);
        Task<string> GetSecretAsync(string vaultKey);
    }
}
