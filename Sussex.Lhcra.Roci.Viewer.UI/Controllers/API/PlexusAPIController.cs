using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlexusAPIController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {

            return RedirectToAction("index", "Home");
        }
    }
}
