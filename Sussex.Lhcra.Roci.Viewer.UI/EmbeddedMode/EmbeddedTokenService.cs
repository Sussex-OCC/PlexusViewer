using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Common.Domain.Constants;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.EmbeddedMode
{
    public class EmbeddedTokenService : ITokenService
    {
        private readonly IDownStreamAuthorisation _downStreamAuthorisation;

        public EmbeddedTokenService(IDownStreamAuthorisation downStreamAuthorisation)
        {
            _downStreamAuthorisation = downStreamAuthorisation;
        }

        public Task<string> GetLoggingOrAuditToken(string scope)
        {
            return Task.FromResult("hello");
        }

        public string GetRoleFromToken()
        {
            return "GetRoleFromToken";
        }

        public string GetSystemIdentifier()
        {
            return "GetSystemIdentifier";
        }

        public async Task<string> GetTokenOnBehalfOfUserOrSystem(IAzureADSettings azureADSettings)
        {
            return await _downStreamAuthorisation.GetAccessToken("326d473f-ae9b-4f69-86bc-bc0b0195bc36", "c18276df-d6a4-4f9c-9d46-1e84068bc7a3", "~5tk5L07tCoJqc~f2Do_ud-YcEV1f1-Q6L", "20d55975-9334-41b4-8b89-526c2d20ab6d");
        }

        public string GetTokenString()
        {
            return "GetTokenString";
        }

        public string GetUsername()
        {
            return "GetUsername";
        }

        public RoleIdType GetUserRole()
        {
            return RoleIdType.Clinician;
        }
    }
}
