using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{
    public interface ISpineModelBuilder
    {
        Task  FillUserDetailsFromAzureAsync(PatientCareRecordRequestDomainModel spineModel);
    }
}