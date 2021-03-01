using System;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public interface ISmspProxyDataService
    {
        Task<string> GetDataContent(string url, string correlationId, string organisationAsId);
    }
}