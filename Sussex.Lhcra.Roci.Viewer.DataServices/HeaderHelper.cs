using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class HeaderHelper
    {
        public static void AddCorrelation(string correlationId, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("x-correlationId", correlationId);
        }

        public static void AddOrganisationAsId(string origanisationAsId, HttpClient httpClient)
        {
            httpClient.DefaultRequestHeaders.Add("x-organisationAsId", origanisationAsId);
        }
    }
}
