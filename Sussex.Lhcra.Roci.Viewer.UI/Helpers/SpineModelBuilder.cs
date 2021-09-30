using Sussex.Lhcra.Roci.Viewer.Domain;
using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{
    public class SpineModelBuilder : ISpineModelBuilder
    {
        private readonly IGraphProvider graphProvider;

        public SpineModelBuilder(IGraphProvider graphProvider)
        {
            this.graphProvider = graphProvider;
        }

        public async Task FillUserDetailsFromAzureAsync(PatientCareRecordRequestDomainModel spineModel)
        {
            var userId = "";
#if DEBUG
            userId = "karthickv";
#endif
            var userDeets = await graphProvider.GetUserDetails(userId);
            spineModel.PractitionerGivenName = userDeets;
        }

       
    }
}