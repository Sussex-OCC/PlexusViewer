using Newtonsoft.Json;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Common.Domain.Logging.Models;
using Sussex.Lhcra.Common.Domain.Logging.Services;
using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class RociGatewayDataService : IRociGatewayDataService
    {
        private readonly ILoggingDataService _loggingDataService;

        public RociGatewayDataService(ILoggingDataService loggingDataService)
        {
            _loggingDataService = loggingDataService;
        }

        public async Task<PatientCareRecordBundleDomainModel> GetDataContentAsync(string endPoint, string controllerName, PatientCareRecordRequestDomainModel model)
        {
            PatientCareRecordBundleDomainModel result = null;

            try
            {
                var strBody = JsonConvert.SerializeObject(model);
                var fullEndPoint = endPoint + controllerName;
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Post, fullEndPoint))

                {
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

                        await LogResponse(
                            model.OrganisationAsId, 
                            responseContent, 
                            new Guid(model.CorrelationId),
                            "Roci Proxy API",
                            fullEndPoint,
                            response, 
                            (int)response.StatusCode);
                    }
                }

                return result;
            }
            catch(Exception ex)
            {

                return null;
            }
        }

        private async Task<bool> LogResponse(
            string organisationAsId, 
            string content,
            Guid correlationId, 
            string applicationName,
            string endpoint, 
            HttpResponseMessage response, 
            int statusCode)
        {

            try
            {
                var logRecord = new LogRecordRequestModel
                {
                    AppName = applicationName,
                    CorrelationId = correlationId,
                    OrganisationAsId = organisationAsId,
                    RequestMethod = LogConstants.RequestType.HttpPost,
                    MessageType = LogConstants.MessageType.Response,
                    Endpoint = endpoint,
                    MessageBody = content,
                    MessageHeader = JsonConvert.SerializeObject(response.Headers),
                    StatusCode = statusCode
                };


                await _loggingDataService.LogRecordAsync(logRecord);
            }
            catch
            {
                return false;
            }


            return true;
        }
    }
}
