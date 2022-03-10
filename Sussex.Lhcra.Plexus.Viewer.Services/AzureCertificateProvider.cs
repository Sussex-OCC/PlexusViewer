using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Sussex.Lhcra.Plexus.Viewer.Services.Core;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.Services
{
    public class AzureCertificateProvider : ICertificateProvider
    {
        private readonly CertificateClient _certificateClient;
        private readonly SecretClient _secretClient;

        public AzureCertificateProvider(CertificateClient certificateClient, SecretClient secretClient)
        {
            _certificateClient = certificateClient;
            _secretClient = secretClient;
        }

        public async Task<X509Certificate2> GetCertificate(string certificateName)
        {
            var certificateResponse = await _certificateClient.DownloadCertificateAsync(certificateName);

            return certificateResponse.Value;
        }

        public async Task<string> GetSecretAsync(string vaultKey)
        {
            var returnValue = "";

            try
            {
                var secret = await _secretClient.GetSecretAsync(vaultKey);

                returnValue = secret.Value.Value;
            }
            catch (Exception)
            {
                returnValue = "";
            }

            return returnValue;

        }

        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];

            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

    }
}
