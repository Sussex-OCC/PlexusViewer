using System.Collections.Generic;
using System.Threading.Tasks;
using Sussex.Lhcra.Plexus.Viewer.Domain.Models;

namespace Sussex.Lhcra.Plexus.Viewer.Domain
{
    public interface IGraphProvider
    {
        Task<PlexusUser> GetLoggedInUserDetails(IEnumerable<string> properties);
    }
}