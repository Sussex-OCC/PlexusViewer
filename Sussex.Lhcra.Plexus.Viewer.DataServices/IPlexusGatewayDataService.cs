using Sussex.Lhcra.Plexus.Viewer.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.DataServices
{
    public interface IPlexusGatewayDataService
    {
        Task<IEnumerable<PatientCarePlanRecord>> GetCarePlanDataContentAsync(string endPoint, string controllerName, string correlationId, string organisationAsId, PatientCareRecordRequestDomainModel model);
        Task<PatientCareRecordBundleDomainViewModel> GetDataContentAsync(string endPoint, string controllerName, string correlationId, string organisationAsId, PatientCareRecordRequestDomainModel model);
    }
}