using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.Domain
{
    public interface IGraphProvider
    {
        Task<string> GetUserDetails(string userId);
        Task<string> GetIdByEmail(string email);
        Task<string> GetIdByGroupName(string groupName);

    }
}