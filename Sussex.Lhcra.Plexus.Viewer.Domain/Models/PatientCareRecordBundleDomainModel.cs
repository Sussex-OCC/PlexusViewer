using System.Net;

namespace Sussex.Lhcra.Plexus.Viewer.Domain.Models
{
    public class PatientCareRecordBundleDomainViewModel
    {
        public HttpStatusCode StatusCode { get; set; }

        public string ErrorCode { get; set; }

        public string StrBundle { get; set; }

        public string Content { get; set; }

        public string CorrelationId { get; set; }

        public string Message { get; set; }
    }
}
