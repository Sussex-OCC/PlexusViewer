using Microsoft.Extensions.Options;
using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Roci.Viewer.UI.Configurations;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.EmbeddedMode
{
    public class EmbeddedTokenService : ITokenService
    {
        private readonly IDownStreamAuthorisation _downStreamAuthorisation;
        private readonly EmbeddedTokenConfig _embeddedTokenConfig;

        public EmbeddedTokenService(IDownStreamAuthorisation downStreamAuthorisation, IOptions<EmbeddedTokenConfig> embeddedTokenOption)
        {
            _downStreamAuthorisation = downStreamAuthorisation;
            _embeddedTokenConfig = embeddedTokenOption.Value;
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
            return await _downStreamAuthorisation.GetAccessToken(_embeddedTokenConfig.TenantId, _embeddedTokenConfig.ClientId, _embeddedTokenConfig.Secret, azureADSettings.SystemToSystemScope);
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
