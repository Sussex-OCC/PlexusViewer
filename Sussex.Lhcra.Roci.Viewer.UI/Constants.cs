﻿using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sussex.Lhcra.Plexus.Viewer.UI
{
    public static class Constants
    {
        public const string ViewerSessionKeyName = "Plexus.Viewer.Session";
        public const string ViewerSessionLoggedIn = "Plexus.Viewer.Session.LoggedIn";
        public const string ViewerSessionDemographicDiv = "Plexus.Viewer.Session.Demographics";
        public const string ViewerSessionUrlParametersKeyName = "Plexus.Viewer.Session.UrlParameters";

        public const string UserDetailsSessionKeyName = "Plexus.Viewer.UserSession";

        public const string ClinicianRole = "CLINICIAN";
        public const string UnknownRole = "UNK";//"SUSSEX MENTAL HEALTH SERVICES (INTEGRATIONS)”"
        public const string SPFT = "SUSSEX MENTAL HEALTH SERVICES (INTEGRATIONS)";

        public const string Summary = "Summary";
        public const string ProblemsAndIssues = "ProblemsAndIssues";
        public const string Medication = "Medication";
        public const string Allergies = "Allergies";
        public const string Encounters = "Encounters";
        public const string Observations = "Observations";
        public const string Referrals = "Referrals";
        public const string Clinical = "Clinical";
        public const string Admin = "Admin";
        public const string CommunityCarePlans = "CommunityCarePlans";
        public const string MentalHealthCrisisPlans = "MentalHealthCrisisPlans";


        public const string All = "All";
        public const string Investigations = "Investigations";
        public const string Immunisations = "Immunisations";
        public const string OrganisationOdsCode = "TEST";//OCC ODS CODE
        public const string OrganisationAsId = "000000000";//OCC ORG ASID
    }


    public static class MandatoryFields
    {
        public static readonly IEnumerable<string> UserAzureProperties = new ReadOnlyCollection<string>(new List<string> { "Id", "GivenName", "Surname", "DisplayName", "Companyname", "Department", "EmployeeId", "JobTitle" });

    }
}
