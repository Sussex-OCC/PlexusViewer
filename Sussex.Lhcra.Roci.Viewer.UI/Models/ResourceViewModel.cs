using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;

namespace Sussex.Lhcra.Roci.Viewer.UI.Models
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
    }
}
