using System.Collections.Generic;
using System.Linq;

namespace Sussex.Lhcra.Plexus.Viewer.Domain.Models
{
    public class PlexusUser
    {
        public string UserId { get; set; } //a.Id
        public string Username { get; set; }
        public string UsernamePrefix { get; set; } // a.JobTitle}
        public string GivenName { get; set; }
        public string FamilyName { get; set; }       
        public string OrganisationOdsCode { get; set; } //Azure.EmployeeId
        public string OrganisationAsid { get; set; } //Azure.Department
        public string OrganisationName { get; set; } //azure.company name

        public bool IsValid()
        {
            return GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(string) && string.IsNullOrEmpty((string)p.GetValue(this)) == true)
                    .Select(p => p.Name)
                    .Count() <= 0;
        }

        public IEnumerable<string> MissingProperties()
        {
            return GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(string) && string.IsNullOrEmpty((string)p.GetValue(this)) == true)
                    .Select(p => p.Name).ToList();
        }

    }
}