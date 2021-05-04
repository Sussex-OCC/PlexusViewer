using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers;
using System;
using Sussex.Lhcra.Roci.Viewer.UI.Extensions;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers.Core;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sussex.Lhcra.Roci.Viewer.Services.Core;
using Sussex.Lhcra.Roci.Viewer.UI.Configurations;
using Microsoft.Extensions.Options;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private ICacheService _redisCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _userSession => _httpContextAccessor.HttpContext.Session;
        private readonly ICertificateProvider _appSecretsProvider;
        private readonly ViewerAppSettingsConfiguration _viewerConfiguration;

        public AccountController(ICertificateProvider appSecretsProvider, 
            IHttpContextAccessor httpContextAccessor, IOptions<ViewerAppSettingsConfiguration> configurationOption)
        {
            _viewerConfiguration = configurationOption.Value;
            _appSecretsProvider = appSecretsProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> SignOut(string page)
        {
            
            try
            {
                var redisConn = await _appSecretsProvider.GetSecretAsync(_viewerConfiguration.DatabaseConnectionStrings.RedisCacheConnectionString);
                _redisCache = new CacheService(redisConn);

                var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                var userCacheSessionId = _redisCache.GetValueOrTimeOut<string>(userId);

                if (!string.IsNullOrEmpty(userCacheSessionId))
                    _redisCache.SetValue(userId, "");//Only reason for the try/catch is if redis instance cant be accesed it throws an error that needs catching

                _httpContextAccessor.HttpContext.Session.Set<string>(Constants.ViewerSessionLoggedIn, null);

            }
            catch
            {
            }

            //TODO: Sign out user on azure
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();


        }
    }
}
