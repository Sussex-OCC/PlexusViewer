using System;

namespace Sussex.Lhcra.Roci.Viewer.Services
{
    public class MissingUserDetailsException : Exception
    {
        public MissingUserDetailsException()
        {
        }

        public MissingUserDetailsException(string message) : base(message)
        {
        }

        public MissingUserDetailsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}