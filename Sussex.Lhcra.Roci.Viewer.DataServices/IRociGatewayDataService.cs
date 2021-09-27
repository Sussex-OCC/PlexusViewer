using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public interface IRociGatewayDataService
    {
        Task<IEnumerable<PatientCarePlanRecord>> GetCarePlanDataContentAsync(string endPoint, string controllerName, string correlationId, string organisationAsId, PatientCareRecordRequestDomainModel model);
        Task<PatientCareRecordBundleDomainViewModel> GetDataContentAsync(string endPoint, string controllerName, string correlationId, string organisationAsId, PatientCareRecordRequestDomainModel model);
    }
}