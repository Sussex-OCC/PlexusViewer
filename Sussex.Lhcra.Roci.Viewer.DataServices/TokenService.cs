using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Microsoft.Net.Http.Headers;
using Sussex.Lhcra.Roci.Viewer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class TokenService : ITokenService
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TokenService(ITokenAcquisition tokenAcquisition, IHttpContextAccessor httpContextAccessor)
        {
            _tokenAcquisition = tokenAcquisition;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Used for system to system communication between the app and the service
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public async Task<string> GetLoggingOrAuditToken(string scope)
        {
            return await _tokenAcquisition.GetAccessTokenForAppAsync(scope);
        }

        public string GetRoleAsSystemIdentifier()
        {
            var scopeClaims = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.ToString().Contains("scope:"));
            if (scopeClaims != null)
            {
                return scopeClaims.Value;
            }

            var roleClaims = _httpContextAccessor.HttpContext.User.FindFirst(ClaimConstants.Role);
            if (roleClaims != null)
            {
                return roleClaims.Value;
            }

            return "No system identifier found";

        }

        public async Task<string> GetTokenOnBehalfOfUserOrSystem(IAzureADSettings azureADSettings)
        {

            if (!string.IsNullOrEmpty(_httpContextAccessor.HttpContext.User.GetDisplayName()))
            {
                return await _tokenAcquisition.GetAccessTokenForUserAsync(azureADSettings.UserScope);
            }
            else
            {
                return await _tokenAcquisition.GetAccessTokenForAppAsync(azureADSettings.SystemToSystemScope);
            }
        }

        public string GetTokenString()
        {
            return _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization];
        }

        public string GetUsername()
        {
            return _httpContextAccessor.HttpContext.User.GetDisplayName();
        }
    }
}
