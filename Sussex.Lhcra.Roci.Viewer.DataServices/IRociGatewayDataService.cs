using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public interface IRociGatewayDataService
    {
        Task<PatientCareRecordBundleDomainModel> GetDataContentAsync(string endPoint, string controllerName, PatientCareRecordRequestDomainModel model);
    }
}