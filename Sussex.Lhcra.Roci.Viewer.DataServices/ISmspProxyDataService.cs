using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public interface ISmspProxyDataService
    {
        Task<SpineDataModel> GetDataContent(string url, string correlationId, string organisationAsId);
    }
}