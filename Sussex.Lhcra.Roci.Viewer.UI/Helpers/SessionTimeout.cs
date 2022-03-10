using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Sussex.Lhcra.Plexus.Viewer.Services.Core;
using Sussex.Lhcra.Plexus.Viewer.UI.Configurations;
using Sussex.Lhcra.Plexus.Viewer.UI.Extensions;
using Sussex.Lhcra.Plexus.Viewer.UI.Helpers.Core;
using System;
using System.Security.Claims;

namespace Sussex.Lhcra.Plexus.Viewer.UI.Helpers
{
    public class SessionTimeout : ActionFilterAttribute
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ICacheService _redisCache;
        private ISession _userSession => _httpContextAccessor.HttpContext.Session;
        private readonly ICertificateProvider _appSecretsProvider;
        private readonly ViewerAppSettingsConfiguration _viewerConfiguration;

        public SessionTimeout(ICertificateProvider appSecretsProvider,
            IHttpContextAccessor httpContextAccessor, IOptions<ViewerAppSettingsConfiguration> configurationOption)
        {
            _httpContextAccessor = httpContextAccessor;
            _appSecretsProvider = appSecretsProvider;
            _viewerConfiguration = configurationOption.Value;
        }

        public async override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {

                var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var userSessionLoggedInId = _userSession.Get<string>(Constants.ViewerSessionLoggedIn);

                if (string.IsNullOrEmpty(userSessionLoggedInId)) // User has no session and is not logged in else where 
                {
                    var newSessionId = Guid.NewGuid().ToString();
                    _httpContextAccessor.HttpContext.Session.Set<string>(Constants.ViewerSessionLoggedIn, newSessionId);
                }
                else if (string.IsNullOrEmpty(userSessionLoggedInId))
                {
                    filterContext.Result = new RedirectResult("~/Account/SignOut");
                    return;
                }
            }
            catch (Exception)
            {

            }

            base.OnActionExecuting(filterContext);
        }
    }
}

