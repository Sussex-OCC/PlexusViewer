using Sussex.Lhcra.Roci.Viewer.Domain;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public interface ITokenService
    {
        Task<string> GetTokenOnBehalfOfUserOrSystem(IAzureADSettings azureADSettings);

        Task<string> GetLoggingOrAuditToken(string scope);

        string GetUsername();

        string GetRoleAsSystemIdentifier();

        string GetTokenString();
    }
}
