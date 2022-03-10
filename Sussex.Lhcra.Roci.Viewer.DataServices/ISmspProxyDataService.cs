using Sussex.Lhcra.Plexus.Viewer.Domain.Models;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.DataServices
{
    public interface ISmspProxyDataService
    {
        Task<SpineDataModel> GetDataContent(string url, string correlationId, string organisationAsId);
    }
}