﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.Domain
{
    public interface IGraphProvider
    {
        Task<PlexusUser> GetLoggedInUserDetails(IEnumerable<string> properties);
    }
}