using Newtonsoft.Json;
using Sussex.Lhcra.Smsp.Domain.Models;

namespace Sussex.Lhcra.Roci.Viewer.Domain.Models
{
    public class PatientCareRecordRequestDomainModel : PatientModel
    {
        public PatientCareRecordRequestDomainModel()
        { }

        public PatientCareRecordRequestDomainModel(PatientModel model)
        {
            this.Person = new PersonModel
            {
                Address = new AddressModel
                {
                    AddressLine1 = model.Person.Address.AddressLine1,
                    AddressLine2 = model.Person.Address.AddressLine2,
                    AddressLine3 = model.Person.Address.AddressLine3,
                    AddressLine4 = model.Person.Address.AddressLine4,
                    AddressLine5 = model.Person.Address.AddressLine5
                },

                DateOfBirth = model.Person.DateOfBirth,
                FamilyName = model.Person.FamilyName,
                Gender = model.Person.Gender,
                GivenName1 = model.Person.GivenName1,
                GivenName2 = model.Person.GivenName2,
                Prefix = model.Person.Prefix,
            };

            this.GpPractice = new GpPracticeModel
            {
                Address = new AddressModel
                {
                    AddressLine1 = model.Person.Address.AddressLine1,
                    AddressLine2 = model.Person.Address.AddressLine2,
                    AddressLine3 = model.Person.Address.AddressLine3,
                    AddressLine4 = model.Person.Address.AddressLine4,
                    AddressLine5 = model.Person.Address.AddressLine5
                },
                Name = model.GpPractice.Name,
                OdsCode = model.GpPractice.OdsCode
            };

            NhsNumber = model.NhsNumber;
        }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("organisationAsId")]
        public string OrganisationAsId { get; set; }

        [JsonProperty("organisationOdsCode")]
        public string OrganisationOdsCode { get; set; }

        [JsonProperty("practitionerId")]
        public string PractitionerId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("userToken")]
        public string UserToken { get; set; }
    }
}
