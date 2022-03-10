using Hl7.Fhir.Model;
using Sussex.Lhcra.Plexus.Viewer.Domain.Models;
using System;
using System.Collections.Generic;

namespace Sussex.Lhcra.Plexus.Viewer.UI.Models
{
    public class ResourceViewModel
    {
        public List<CarePlan> CarePlans { get; set; }
        public List<CarePlan> ESHTPlans { get; set; }
        public List<CarePlan> SCFTPlans { get; set; }
        public List<CarePlan> EMISPlans { get; set; }
        public string Div { get; set; }
        public string Detail { get; set; }
        public string Heading { get; set; }
        public string NhsNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Patient Patient { get; set; }
        public string FormattedDateOfBirth { get; set; }
        public int Age { get; set; }
        public string StrAge { get; set; }
        public string Title { get; set; }
        public string DemographicsDiffDiv { get; set; }
        public DemographicsViewModel DemographicsDiffDivModel { get; set; }
        public int DifferencesFound { get; set; }
        public string ActiveView { get; set; }
        public IEnumerable<PatientCarePlanRecord> Plans { get; set; }
    }
}
