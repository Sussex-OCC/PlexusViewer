﻿using System;
using System.Runtime.Serialization;

namespace Sussex.Lhcra.Roci.Viewer.Services
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