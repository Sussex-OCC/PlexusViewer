using System;

namespace Sussex.Lhcra.Roci.Viewer.Domain.Models
{
    public class PatientCarePlanRecord
    {
        public string NHSNumber { get; set; }
        public string PlanType { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
        public string Aim { get; set; }
        public string ProviderName { get; set; }
    }
}
