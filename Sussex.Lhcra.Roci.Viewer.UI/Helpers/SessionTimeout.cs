using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Sussex.Lhcra.Roci.Viewer.UI.Extensions;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers.Core;
using System.Security.Claims;

namespace Sussex.Lhcra.Roci.Viewer.UI.Helpers
{
    public class SessionTimeout : ActionFilterAttribute
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ICacheService _redisCache;
        private ISession _userSession => _httpContextAccessor.HttpContext.Session;

        public SessionTimeout(ICacheService redisCache,IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _redisCache = redisCache;
        }

        public async override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
               
                var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var userCacheSessionId = _redisCache.GetValueOrTimeOut<string>(userId);
                var userSessionLoggedInId = _userSession.Get<string>(Constants.ViewerSessionLoggedIn);

                if (string.IsNullOrEmpty(userSessionLoggedInId) && string.IsNullOrEmpty(userCacheSessionId)) // User has no session and is not logged in else where 
                {
                    var newSessionId = Guid.NewGuid().ToString();
                    _httpContextAccessor.HttpContext.Session.Set<string>(Constants.ViewerSessionLoggedIn, newSessionId);
                    _redisCache.SetValue(userId, newSessionId);
                }
                else if (string.IsNullOrEmpty(userSessionLoggedInId) && !string.IsNullOrEmpty(userCacheSessionId))
                {
                    filterContext.Result = new RedirectResult("~/Account/SessionExpired");
                    return;
                }
                else if (userSessionLoggedInId != userCacheSessionId)
                {
                    filterContext.Result = new RedirectResult("~/Account/UserAlreadyLoggedIn");
                    return;
                }
            }
            catch
            {

            }
           
            base.OnActionExecuting(filterContext);

        }
    }
}

