﻿using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Sussex.Lhcra.Roci.Viewer.Services.Core;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.Services
{
    public class AzureCertificateProvider : ICertificateProvider
    {
        private string _vaultUrl;
        private string _clientId;
        private string _clientSecret;

        public AzureCertificateProvider(string vaultUrl, string clientId, string clientSecret)
        {
            _vaultUrl = vaultUrl;
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<X509Certificate2> GetCertificate(string certificateName)
        {
            var certificateSecret = await GetSecretAsync(certificateName);
            byte[] clientCertBytes = StringToByteArray(certificateSecret);            
            var certificate = new X509Certificate2(clientCertBytes);
            //Certificate pull down to be tested..
            return certificate;
        }

        public async Task<string> GetSecretAsync(string vaultKey)
        {
            var returnValue = "";

            try
            {
                var kv = new KeyVaultClient(async (authority, resource, scope) =>
                {
                    var authContext = new AuthenticationContext(authority);
                    var clientCred = new ClientCredential(_clientId, _clientSecret);

                    var result = await authContext.AcquireTokenAsync(resource, clientCred);

                    if (result == null)
                        throw new InvalidOperationException("Failed to obtain the JWT token");

                    return result.AccessToken;
                });

                var secret = await kv.GetSecretAsync(_vaultUrl, vaultKey);

                returnValue = secret.Value;
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
