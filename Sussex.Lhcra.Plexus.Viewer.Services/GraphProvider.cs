﻿using Microsoft.Graph;
using Microsoft.Identity.Client;
using Sussex.Lhcra.Plexus.Viewer.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.Services
{
    public class GraphProvider : IGraphProvider
    {
        private readonly GraphServiceClient graphServiceClient;

        public GraphProvider(GraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        public async Task<PlexusUser> GetLoggedInUserDetails(IEnumerable<string> properties)
        {
            var azureUser = await graphServiceClient.Me.Request().Select(string.Join(",", properties)).GetAsync();
            return MapToPlexusUser(azureUser);
        }

        private PlexusUser MapToPlexusUser(User azureUser)
        {
            if (azureUser == null)
                throw new UserNullException("Logged in user details from Azure is null");

            return new PlexusUser()
            {
                UserId = azureUser.Id,
                GivenName = azureUser.GivenName,
                FamilyName = azureUser.Surname,
                Username = azureUser.DisplayName, //dont think this is right
                OrganisationName = azureUser.CompanyName,
                OrganisationAsid = azureUser.Department,
                OrganisationOdsCode = azureUser.EmployeeId,
                UsernamePrefix = azureUser.JobTitle
            };

        }
    }
}