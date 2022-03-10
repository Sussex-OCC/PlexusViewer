using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sussex.Lhcra.Plexus.Viewer.Services.Core;
using Sussex.Lhcra.Plexus.Viewer.UI.Configurations;
using Sussex.Lhcra.Plexus.Viewer.UI.Extensions;
using Sussex.Lhcra.Plexus.Viewer.UI.Helpers.Core;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.UI.Controllers
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
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            _httpContextAccessor.HttpContext.Session.Set<string>(Constants.ViewerSessionLoggedIn, null);
            //TODO: Sign out user on azure
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }
    }
}
