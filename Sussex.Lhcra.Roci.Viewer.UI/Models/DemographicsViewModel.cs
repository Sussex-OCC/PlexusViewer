using Sussex.Lhcra.Smsp.Domain.Models;

namespace Sussex.Lhcra.Roci.Viewer.UI.Models
{
    public class DemographicsViewModel
    {
        public string Names { get; set; }
        public string Postcode { get; set; }
        public string Addreses { get; set; }
        public string DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string GivenNames { get; set; }
        public string FamilyNames { get; set; }
        public string Prefixes { get; set; }
        public AddressModel GPPracticeAddress { get; set; }
        public string GPPracticeODSCode { get; set; }
        public bool DifferencesFound { get; set; }
        public string LocalGender { get; set; }
        public string LocalAddreses { get; set; }
        public string LocalPostcode { get; set; }
        public string LocalFamilyNames { get; set; }
        public string LocalDateOfBirth { get; set; }
        public string LocalGPPracticeODSCode { get; set; }
        public string LocalGivenNames { get; set; }
    }
}
