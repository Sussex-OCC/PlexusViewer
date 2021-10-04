﻿using System;

namespace Sussex.Lhcra.Roci.Viewer.Domain
{
    public class PlexusUser
    {
        public string UserId{ get; set; } //a.Id
        public string Username { get; set; }
        public string UsernamePrefix { get; set; } // a.JobTitle}
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string OrganisationOdsCode  { get; set; } //Azure.EmployeeId
        public string OrganisationAsid { get; set; } //Azure.Department
        public string OrganisationName { get; set; } //azure.company name


        //from story:
        //UserId => Azure.UserObjectId (Auto generated by Azure) - cant see user object id from azure
        //UserRole => Assigned to User by creating a UNK role

    }
}