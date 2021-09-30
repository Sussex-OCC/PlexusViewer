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

        public Task<string> GetIdByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetIdByGroupName(string groupName)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetUserDetails(string userId)
        {
            var testUser = await graphServiceClient.Me.Request().GetAsync();
            return testUser.DisplayName;
        }
    }
}
