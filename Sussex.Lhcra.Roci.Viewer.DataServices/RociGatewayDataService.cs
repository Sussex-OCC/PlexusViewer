using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Common.Domain.Logging.Models;
using Sussex.Lhcra.Common.Domain.Logging.Services;
using Sussex.Lhcra.Roci.Viewer.DataServices.Models;
using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class RociGatewayDataService : IRociGatewayDataService
    {
        private readonly ITokenService _tokenService;
        private readonly RociGatewayADSetting _rociGatewayADSetting;
        private readonly LoggingServiceADSetting _loggingServiceADSetting;

        public RociGatewayDataService(
            ITokenService tokenService,
            IOptions<RociGatewayADSetting> rociGatewayOptions, 
            IOptions<LoggingServiceADSetting> loggingServiceOption)
        {
            _tokenService = tokenService;
            _rociGatewayADSetting = rociGatewayOptions.Value;
            _loggingServiceADSetting = loggingServiceOption.Value;
        }

        public async Task<PatientCareRecordBundleDomainModel> GetDataContentAsync(string endPoint, string controllerName, PatientCareRecordRequestDomainModel model)
        {
            PatientCareRecordBundleDomainModel result = null;

            try
            {
                string appToken = await _tokenService.GetToken(_rociGatewayADSetting);

                var strBody = JsonConvert.SerializeObject(model);
                var fullEndPoint = endPoint + controllerName;
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Post, fullEndPoint))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appToken);

                    using (var stringContent = new StringContent(strBody))
                    {
                        request.Content = stringContent;

                        request.Content.Headers.ContentType.MediaType = "application/json";

                        var response = await client.SendAsync(request);

                        var responseContent = "";

                        if (response.IsSuccessStatusCode)
                        {
                            responseContent = await response.Content.ReadAsStringAsync();
                            result = JsonConvert.DeserializeObject<PatientCareRecordBundleDomainModel>(responseContent);
                        }

                        // TODO: Custom logging feature to be added for Roci Viewer/Gateway service. 
                        //await LogResponse(
                        //    model.OrganisationAsId,
                        //    responseContent,
                        //    new Guid(model.CorrelationId),
                        //    "Roci Proxy API",
                        //    fullEndPoint,
                        //    response,
                        //    (int)response.StatusCode);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        // TODO: Custom logging feature to be added for Roci Viewer/Gateway service. 
        //private async Task<bool> LogResponse(
        //    string organisationAsId,
        //    string content,
        //    Guid correlationId,
        //    string applicationName,
        //    string endpoint,
        //    HttpResponseMessage response,
        //    int statusCode)
        //{

        //    try
        //    {
        //        var loggingToken = await _tokenService.GetLoggingOrAuditToken(_loggingServiceADSetting.SystemToSystemScope);
                
        
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
