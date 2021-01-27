using Sussex.Lhcra.Roci.Viewer.Domain;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public interface ITokenService
    {
        Task<string> GetToken(IAzureADSettings azureADSettings);

        Task<string> GetLoggingOrAuditToken(string scope);
    }
}
