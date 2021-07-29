using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.Models
{
    public class UrlParemetersModel
    {
        private bool HasValidationErrors { get; set; }
        private string NHSNumber { get; set; }
        private DateTime? DateOfBirth { get; set; }
        private string OrganisationASID { get; set; }
        private string OrganisationODScode { get; set; }
        private string UserId { get; set; }
        private string UserName { get; set; }
        private string UserRole { get; set; }
        private string SessionId { get; set; }
        private string CorrelationId { get; set; }
        private string PatientGivenName { get; set; }
        private string PatientFamilyName { get; set; }
        private string PatientPostCode { get; set; }
        private string PatientGender { get; set; }
        private string PatientPracticeOdsCode { get; set; }
        private string PatientAddress { get; set; }
        private string PractitionerNamePrefix { get; set; }
        private string PractitionerFamilyName { get; set; }
        private string PractitionerGivenName { get; set; }

        private string SdsUserId { get; set; }
        private string RequestorId { get; set; }


        public UrlParemetersModel AddNHSNumber(string val)
        {
            NHSNumber = val;
            return this;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(NHSNumber))
                return false;

            if (string.IsNullOrEmpty(OrganisationASID))
                return false;

            if (string.IsNullOrEmpty(OrganisationODScode))
                return false;

            if (string.IsNullOrEmpty(UserId))
                return false;

            if (string.IsNullOrEmpty(UserName))
                return false;

            if (string.IsNullOrEmpty(UserRole))
            {
                return false;
            }
            else
            {
                if(UserRole.Trim().ToUpper() != Constants.ClinicianRole && UserRole.Trim().ToUpper() != Constants.UnknownRole)
                {
                    return false;
                }
            }
                

            if (string.IsNullOrEmpty(SessionId))
                return false;

            if (string.IsNullOrEmpty(CorrelationId))
                return false;

            if (string.IsNullOrEmpty(PatientGivenName))
                return false;

            if (string.IsNullOrEmpty(PatientFamilyName))
                return false;

            if (string.IsNullOrEmpty(PatientPostCode))
                return false;

            if (string.IsNullOrEmpty(PatientGender))
                return false;

            if (string.IsNullOrEmpty(PatientPracticeOdsCode))
                return false;

            if (string.IsNullOrEmpty(PatientAddress))
                return false;

            if (string.IsNullOrEmpty(PractitionerNamePrefix))
                return false;

            if (string.IsNullOrEmpty(PractitionerGivenName))
                return false;

            if (string.IsNullOrEmpty(PractitionerFamilyName))
                return false;

            if (string.IsNullOrEmpty(SdsUserId))
                return false;

            if (string.IsNullOrEmpty(RequestorId))
                return false;

            /*string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress*/

            return !HasValidationErrors;
        }

        public UrlParemetersModel AddDateOfBirth(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                HasValidationErrors = true;
                return this;
            }

            DateTime date;

            if (DateTime.TryParseExact(val, "dd-MM-yyyy",
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.None,
                                       out date))
            {
                DateOfBirth = date;
            }
            else
            {
                HasValidationErrors = true;
            }
           
            return this;

        }

        

        public UrlParemetersModel AddOrganisationASID(string val)
        {
            OrganisationASID = val;
            return this;
        }

        public UrlParemetersModel AddOrganisationODScode(string val)
        {
            OrganisationODScode = val;
            return this;
        }

        public UrlParemetersModel AddUserId(string val)
        {
            UserId = val;
            return this;
        }

        public UrlParemetersModel AddUserName(string val)
        {
            UserName = val;
            return this;
        }

        public UrlParemetersModel AddUserRole(string val)
        {
            UserRole = val;
            return this;
        }

        public UrlParemetersModel AddSessionId(string val)
        {
            SessionId = val;
            return this;
        }

        public UrlParemetersModel AddCorrelationId(string val)
        {
            CorrelationId = val;
            return this;
        }

        public UrlParemetersModel AddPatientGivenName(string val)
        {
            PatientGivenName = val;
            return this;
        }

        public UrlParemetersModel AddPatientFamilyName(string val)
        {
            PatientFamilyName = val;
            return this;
        }

        public UrlParemetersModel AddPatientPostCode(string val)
        {
            PatientPostCode = val;
            return this;
        }

        public UrlParemetersModel AddPatientGender(string val)
        {
            PatientGender = val;
            return this;
        }

        public UrlParemetersModel AddPatientPracticeOdsCode(string val)
        {
            PatientPracticeOdsCode = val;
            return this;
        }

        public UrlParemetersModel AddPractitionerNamePrefix(string val)
        {
            PractitionerNamePrefix = val;
            return this;
        }

        public UrlParemetersModel AddPractitionerFamilyName(string val)
        {
            PractitionerFamilyName = val;
            return this;
        }

        public UrlParemetersModel AddRequestorId(string val)
        {
            RequestorId = val;
            return this;
        }

        public UrlParemetersModel AddSdsUserId(string val)
        {
            SdsUserId = val;
            return this;
        }

        public UrlParemetersModel AddPractitionerGivenName(string val)
        {
            PractitionerGivenName = val;
            return this;
        }

        public UrlParemetersModel AddPatientAddress(string val)
        {
            PatientAddress = val;
            return this;
        }


    }
}
