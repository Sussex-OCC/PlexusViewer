using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers;
using System;
using Sussex.Lhcra.Roci.Viewer.UI.Extensions;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers.Core;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private ICacheService _redisCache;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _userSession => _httpContextAccessor.HttpContext.Session;

        public AccountController(ICacheService redisCache, IHttpContextAccessor httpContextAccessor)
        {
            _redisCache = redisCache;
            _httpContextAccessor = httpContextAccessor;
        }

    
        [HttpGet]
        public IActionResult SessionExpired()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                if (!string.IsNullOrEmpty(userId))
                    _redisCache.SetValue(userId, "");
            }
            catch
            {
            }

            return View();

        }

        [HttpGet]
        public IActionResult UserAlreadyLoggedIn()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                if (!string.IsNullOrEmpty(userId))
                    _redisCache.SetValue(userId, "");
            }
            catch
            {
            }

            return View();
        }

        [HttpGet]
        public IActionResult SignOut(string page)
        {
            return View();
        }
    }
}
