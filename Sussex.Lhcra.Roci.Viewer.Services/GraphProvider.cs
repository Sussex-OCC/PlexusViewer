using Microsoft.Graph;
using Sussex.Lhcra.Roci.Viewer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.Services
{
    public class GraphProvider : IGraphProvider
    {
        private readonly GraphServiceClient graphServiceClient;

        public GraphProvider(GraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        public async Task<PlexusUser> GetLoggedInUserDetails(IList<string> properties)
        {
            var azureUser = await graphServiceClient.Me.Request().Select(string.Join(",", properties)).GetAsync();

            return MapToPlexusUser(azureUser);
        }

        private PlexusUser MapToPlexusUser(User azureUser)
        {
            if (azureUser == null)
                throw new Exception("Logged in user details from Azure is null");

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
