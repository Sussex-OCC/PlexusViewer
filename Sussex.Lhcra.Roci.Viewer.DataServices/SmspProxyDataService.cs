using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Common.Domain.Logging.Models;
using Sussex.Lhcra.Common.Domain.Logging.Services;
using Sussex.Lhcra.Roci.Viewer.DataServices.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Sussex.Lhcra.Common.AzureADServices.Interfaces;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class SmspProxyDataService : ISmspProxyDataService
    {
        private const string ServiceName = "Spine Proxy";

        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly RociGatewayADSetting _rociGatewayADSetting;
        private readonly LoggingServiceADSetting _loggingServiceADSetting;

        public SmspProxyDataService(
            HttpClient httpClient, 
            ITokenService tokenService,
            IOptions<RociGatewayADSetting> rociGatewayOptions, 
            IOptions<LoggingServiceADSetting> loggingServiceOption)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _rociGatewayADSetting = rociGatewayOptions.Value;
            _loggingServiceADSetting = loggingServiceOption.Value;
        }

        public async Task<string> GetDataContent(string url, string correlationId, string organisationAsId)
        {
            string token = await _tokenService.GetTokenOnBehalfOfUserOrSystem(_rociGatewayADSetting);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HeaderHelper.AddCorrelation(correlationId, _httpClient);
            HeaderHelper.AddOrganisationAsId(organisationAsId, _httpClient);
            HeaderHelper.AddAppDomainTypeId("Plexus", _httpClient);
            HeaderHelper.AddSystemIdentifier("TODO: Add System Identifier", _httpClient);
            HeaderHelper.AddUserRoleId(RoleIdType.Clinician.ToString(), _httpClient);

            var loggingToken = await _tokenService.GetLoggingOrAuditToken(_loggingServiceADSetting.SystemToSystemScope);

            // TODO: Custom logging feature to be added for Roci Viewer/Gateway service. 
            // await LogRequest(new HttpRequestMessage(), url, url, correlationId, ServiceName, _httpClient.BaseAddress.ToString(), loggingToken);

            var httpResponse = await _httpClient.GetAsync(url);
            var returnData = await httpResponse.Content.ReadAsStringAsync();

            // TODO: Custom logging feature to be added for Roci Viewer/Gateway service. 
            //await LogResponse(
            //    url,
            //    returnData,
            //    correlationId,
            //    ServiceName,
            //    _httpClient.BaseAddress.ToString(),
            //    httpResponse,
            //    (int)httpResponse.StatusCode,
            //    loggingToken);

            return returnData;
        }

        // TODO: Custom logging feature to be added for Roci Viewer/Gateway service. 
        //private async Task<bool> LogRequest(
        //    HttpRequestMessage request,
        //    string organisationAsId,
        //    string content,
        //    Guid correlationId,
        //    string applicationName,
        //    string endpoint, 
        //    string loggingToken)
        //{
        //    var logRecord = new LogRecordRequestModel
        //    {
        //        AppName = applicationName,
        //        CorrelationId = correlationId,
        //        OrganisationAsId = organisationAsId,
        //        RequestMethod = LogConstants.RequestType.HttpPost,
        //        MessageType = LogConstants.MessageType.Request,
        //        Endpoint = endpoint,
        //        MessageBody = content,
        //        Exception = string.Empty,
        //        MessageHeader = JsonConvert.SerializeObject(request.Headers)
        //    };

        //    try
        //    {
        //        await _loggingDataService.LogRecordAsync(logRecord, loggingToken);
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        // TODO: Custom logging feature to be added for Roci Viewer/Gateway service. 
        //private async Task<bool> LogResponse(
        //    string organisationAsId,
        //    string content,
        //    Guid correlationId,
        //    string applicationName,
        //    string endpoint,
        //    HttpResponseMessage response,
        //    int statusCode,
        //    string loggingToken)
        //{

        //    try
        //    {
        //        var logRecord = new LogRecordRequestModel
        //        {
        //            AppName = applicationName,
        //            CorrelationId = correlationId,
        //            OrganisationAsId = organisationAsId,
        //            RequestMethod = LogConstants.RequestType.HttpPost,
        //            MessageType = LogConstants.MessageType.Response,
        //            Endpoint = endpoint,
        //            MessageBody = content,
        //            MessageHeader = JsonConvert.SerializeObject(response.Headers),
        //            StatusCode = statusCode
        //        };


        //        await _loggingDataService.LogRecordAsync(logRecord, loggingToken);
        //    }
        //    catch
        //    {
        //        return false;
        //    }


        //    return true;
        //}
    }
}
