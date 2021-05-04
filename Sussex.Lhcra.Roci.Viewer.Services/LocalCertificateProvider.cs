using Sussex.Lhcra.Roci.Viewer.Services.Core;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.Services
{
    public class LocalCertificateProvider : ICertificateProvider
    {
        public Task<X509Certificate2> GetCertificate(string certificateName)
        {
            var certificates = GetLocalMachineCertificates(certificateName);
            return Task.FromResult(certificates[0]);
        }

        public Task<string> GetSecretAsync(string vaultKey)
        {
            throw new NotImplementedException();
        }

        private static X509Certificate2Collection GetLocalMachineCertificates(string serialNumber)
        {
            var localMachineStore = new X509Store(StoreLocation.LocalMachine);
            localMachineStore.Open(OpenFlags.ReadOnly);
            var certificates = localMachineStore.Certificates.Find(X509FindType.FindBySerialNumber, serialNumber, false);
            localMachineStore.Close();
            return certificates;
        }
    }
}
