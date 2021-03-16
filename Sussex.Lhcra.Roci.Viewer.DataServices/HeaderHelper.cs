using System.Net.Http;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class HeaderHelper
    {
        public static void AddCorrelation(string correlationId, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("X-CorrelationId", correlationId);
        }

        public static void AddOrganisationAsId(string origanisationAsId, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("X-OrganisationAsId", origanisationAsId);
        }

        public static void AddSystemIdentifier(string systemIdentifier, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("X-SystemIdentifier", systemIdentifier);
        }

        public static void AddUserRoleId(string userRoleId, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("X-UserRoleId", userRoleId);
        }
        
        public static void AddAppDomainTypeId(string appDomainType, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("X-AppDomainType", appDomainType);
        }
    }
}
