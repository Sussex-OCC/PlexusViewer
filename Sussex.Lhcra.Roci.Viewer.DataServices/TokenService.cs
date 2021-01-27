using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Sussex.Lhcra.Roci.Viewer.Domain;
using System;
using System.Collections.Generic;
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

        public async Task<string> GetToken(IAzureADSettings azureADSettings)
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
    }
}
