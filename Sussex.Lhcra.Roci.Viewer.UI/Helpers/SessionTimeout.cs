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

namespace Sussex.Lhcra.Roci.Viewer.UI.Helpers
{
    public class PlexusAuthorizeAttribute : TypeFilterAttribute
    {
        public PlexusAuthorizeAttribute(string permission)
            : base(typeof(PlexusAuthorizeActionFilter))
        {
            Arguments = new object[] { permission };
        }
    }
    public class PlexusAuthorizeActionFilter : IAuthorizationFilter
    {
        private readonly string _permission;

        public PlexusAuthorizeActionFilter(string permission)
        {
            _permission = permission;
        }

        public PlexusAuthorizeActionFilter()
        {
            
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            bool isAuthorized = CheckUserPermission(_permission);

            if (!isAuthorized)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        private bool CheckUserPermission(string permission)
        {
            // Logic for checking the user permission goes here. 

            var loggedInUser = !string.IsNullOrEmpty(permission);

            // Let's assume this user has only read permission.
            return loggedInUser;
        }
    }

    public class SessionTimeout : ActionFilterAttribute
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _userSession => _httpContextAccessor.HttpContext.Session;

        public SessionTimeout(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            await _userSession.LoadAsync();

            var userSessionLoggedIn = _userSession.Get<string>(Constants.ViewerSessionLoggedIn);

            if (userSessionLoggedIn == null)
            {
                filterContext.Result = new RedirectResult("~/Account/SessionLogin");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

       
    }
}

