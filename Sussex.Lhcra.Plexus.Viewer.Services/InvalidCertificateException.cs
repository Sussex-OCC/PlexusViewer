using System;

namespace Sussex.Lhcra.Plexus.Viewer.Services
{
    public class InvalidCertificateException : Exception
    {
        public InvalidCertificateException()
        {
        }

        public InvalidCertificateException(string message) : base(message)
        {
        }

        public InvalidCertificateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}