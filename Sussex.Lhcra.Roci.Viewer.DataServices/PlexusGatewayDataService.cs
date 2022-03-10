using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Common.Domain.Logging.Models;
using Sussex.Lhcra.Plexus.Viewer.DataServices.Models;
using Sussex.Lhcra.Plexus.Viewer.Domain.Interfaces;
using Sussex.Lhcra.Plexus.Viewer.Domain.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.DataServices
{
    public class PlexusGatewayDataService : IPlexusGatewayDataService
    {
        private readonly ITokenService _tokenService;
        private readonly IIpAddressProvider _ipAddressProvider;
        private readonly ILoggingTopicPublisher _loggingTopicPublisher;
        private readonly HttpClient _httpClient;
        private readonly PlexusGatewayAdSetting _plexusGatewayADSetting;

        public PlexusGatewayDataService(
            ITokenService tokenService,
            IOptions<PlexusGatewayAdSetting> rociGatewayOptions,
            IIpAddressProvider ipAddressProvider,
            ILoggingTopicPublisher loggingTopicPublisher,
            HttpClient httpClient)
        {
            _tokenService = tokenService;
            _ipAddressProvider = ipAddressProvider;
            _loggingTopicPublisher = loggingTopicPublisher;
            _httpClient = httpClient;
            _plexusGatewayADSetting = rociGatewayOptions.Value;
        }

        public async Task<IEnumerable<PatientCarePlanRecord>> GetCarePlanDataContentAsync(string endPoint, string controllerName, string correlationId, string organisationAsId, PatientCareRecordRequestDomainModel model)
        {
            IList<PatientCarePlanRecord> result = null;

            try
            {
                string appToken = await _tokenService.GetTokenOnBehalfOfUserOrSystem(_plexusGatewayADSetting);

                var strBody = JsonConvert.SerializeObject(model);
                var fullEndPoint = endPoint + controllerName + "/" + model.NhsNumber;

                using (var request = new HttpRequestMessage(HttpMethod.Get, fullEndPoint))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appToken);

                    HeaderHelper.AddCorrelation(correlationId, _httpClient);
                    HeaderHelper.AddOrganisationAsId(organisationAsId, _httpClient);

                    await Log(
                         model.OrganisationAsId,
                         strBody,
                         new Guid(correlationId),
                          "Plexus Viewer",
                         fullEndPoint,
                         model,
                         JsonConvert.SerializeObject(_httpClient.DefaultRequestHeaders),
                         PlexusConstants.RequestType.HttpGet);

                    using (var stringContent = new StringContent(strBody))
                    {
                        request.Content = stringContent;

                        request.Content.Headers.ContentType.MediaType = "application/json";

                        var response = await _httpClient.SendAsync(request);

                        var responseContent = "";

                        if (response.IsSuccessStatusCode)
                        {
                            responseContent = await response.Content.ReadAsStringAsync();
                            result = JsonConvert.DeserializeObject<List<PatientCarePlanRecord>>(responseContent);
                        }

                        await Log(
                            model.OrganisationAsId,
                            responseContent,
                            new Guid(correlationId),
                             "Plexus Viewer",
                            fullEndPoint,
                            model,
                            JsonConvert.SerializeObject(response.Headers),
                            PlexusConstants.RequestType.HttpGet);
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<PatientCareRecordBundleDomainViewModel> GetDataContentAsync(string endPoint, string controllerName, string correlationId, string organisationAsId, PatientCareRecordRequestDomainModel model)
        {
            PatientCareRecordBundleDomainViewModel result = null;

            try
            {
                string appToken = await _tokenService.GetTokenOnBehalfOfUserOrSystem(_plexusGatewayADSetting);

                var strBody = JsonConvert.SerializeObject(model);
                var fullEndPoint = endPoint + controllerName;

                using (var request = new HttpRequestMessage(HttpMethod.Get, fullEndPoint))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
                    HeaderHelper.AddCorrelation(correlationId, _httpClient);
                    HeaderHelper.AddOrganisationAsId(organisationAsId, _httpClient);

                    await Log(
                        model.OrganisationAsId,
                        strBody,
                        new Guid(correlationId),
                        "Plexus Viewer",
                        fullEndPoint,
                        model,
                        JsonConvert.SerializeObject(_httpClient.DefaultRequestHeaders),
                        PlexusConstants.RequestType.HttpGet);

                    using (var stringContent = new StringContent(strBody))
                    {
                        request.Content = stringContent;

                        request.Content.Headers.ContentType.MediaType = "application/json";

                        var response = await _httpClient.SendAsync(request);

                        var responseContent = "";

                        if (response.IsSuccessStatusCode)
                        {
                            responseContent = await response.Content.ReadAsStringAsync();
                            result = new PatientCareRecordBundleDomainViewModel
                            {
                                Content = responseContent,
                                StrBundle = responseContent,
                                StatusCode = response.StatusCode,
                                ErrorCode = "",
                                Message = "Retrieve patient data was successful",
                            };
                        }
                        else
                        {
                            responseContent = await response.Content.ReadAsStringAsync();

                            result = new PatientCareRecordBundleDomainViewModel
                            {
                                Content = "",
                                StrBundle = "",
                                StatusCode = response.StatusCode,
                                ErrorCode = response.StatusCode.ToString(),
                                Message = responseContent,
                            };
                        }

                        await Log(
                            model.OrganisationAsId,
                            responseContent,
                            new Guid(correlationId),
                            "Plexus Viewer",
                            fullEndPoint,
                            model,
                            JsonConvert.SerializeObject(response.Headers),
                            PlexusConstants.RequestType.HttpGet);
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }


        private async Task<bool> Log(
            string organisationAsId,
            string content,
            Guid correlationId,
            string applicationName,
            string endpoint,
            PatientCareRecordRequestDomainModel patientCareRecordRequest,
            string headerjson,
            string requestType)
        {

            try
            {
                var plexusLog = new PlexusLogRequestModel
                {
                    AppDomainType = AppDomainType.Plexus,
                    AppName = applicationName,
                    CorrelationId = correlationId,
                    NhsNumber = patientCareRecordRequest.NhsNumber,
                    ClientIpAddress = _ipAddressProvider.GetClientIpAddress(),
                    ServerIpAddress = _ipAddressProvider.GetHostIpAddress(),
                    OrganisationAsId = organisationAsId,
                    OrganisationOdsCode = patientCareRecordRequest.OrganisationOdsCode,
                    Username = _tokenService.GetUsername(),
                    UserRoleId = (int)_tokenService.GetUserRole(),
                    SystemIdentifier = _tokenService.GetSystemIdentifier(),
                    ServiceEvent = "Read",
                    RequestType = requestType,
                    Resource = endpoint,
                    RequestBody = content,
                    PracticeOdsCode = patientCareRecordRequest.GpPractice.OdsCode,
                    RequestHeader = headerjson
                };

                await _loggingTopicPublisher.PublishAsync(
                      plexusLog,
                      correlationId,
                      Lchra.MessageBroker.Common.Messages.MessageType.LogMessage.PlexusLog);
            }
            catch
            {
                return false;
            }


            return true;
        }
    }
}
