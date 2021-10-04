using System;

namespace Sussex.Lhcra.Roci.Viewer.Services
{
    public class UserNullException : Exception
    {
        public UserNullException()
        {
        }

        public UserNullException(string message) : base(message)
        {
        }

        public UserNullException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}