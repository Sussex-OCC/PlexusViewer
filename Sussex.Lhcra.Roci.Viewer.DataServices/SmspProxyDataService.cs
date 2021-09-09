﻿using Microsoft.Extensions.Options;
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
using Sussex.Lhcra.Roci.Viewer.Domain.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Roci.Viewer.Domain.Models;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class SmspProxyDataService : ISmspProxyDataService
    {
        private const string ServiceName = "Spine Proxy";

        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IIpAddressProvider _ipAddressProvider;
        private readonly ILoggingTopicPublisher _loggingTopicPublisher;
        private readonly RociGatewayADSetting _rociGatewayADSetting;

        public SmspProxyDataService(
            HttpClient httpClient,
            ITokenService tokenService,
            IIpAddressProvider ipAddressProvider,
            IOptions<RociGatewayADSetting> rociGatewayOptions,
            ILoggingTopicPublisher loggingTopicPublisher)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _ipAddressProvider = ipAddressProvider;
            _loggingTopicPublisher = loggingTopicPublisher;
            _rociGatewayADSetting = rociGatewayOptions.Value;
        }

        public async Task<SpineDataModel> GetDataContent(string url, string correlationId, string organisationAsId)
        {
            string token = await _tokenService.GetTokenOnBehalfOfUserOrSystem(_rociGatewayADSetting);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HeaderHelper.AddCorrelation(correlationId, _httpClient);
            HeaderHelper.AddOrganisationAsId(organisationAsId, _httpClient);
            HeaderHelper.AddAppDomainTypeId("Plexus", _httpClient);
            HeaderHelper.AddSystemIdentifier("Plexus Viewer", _httpClient);
            HeaderHelper.AddUserRoleId(RoleIdType.Clinician.ToString(), _httpClient);

            await Log(organisationAsId, string.Empty, new Guid(correlationId), "Plexus Viewer", url, JsonConvert.SerializeObject(_httpClient.DefaultRequestHeaders), PlexusConstants.RequestType.HttpGet);

            var httpResponse = await _httpClient.GetAsync(url);

            var returnData = await httpResponse.Content.ReadAsStringAsync();

            var spineDataModel = new SpineDataModel();

            await Log(organisationAsId, returnData, new Guid(correlationId), "Plexus Viewer", url, JsonConvert.SerializeObject(httpResponse.Headers), PlexusConstants.RequestType.HttpGet);

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                spineDataModel.isValid = true;
                spineDataModel.SpineData = returnData;
                return spineDataModel;
            }
            else
            {
                spineDataModel.isValid = false;
                spineDataModel.SpineData = returnData;
                spineDataModel.ErrorMessage = returnData;
                return spineDataModel;
            }
        }

        private async Task<bool> Log(string organisationAsId, string content,
                                    Guid correlationId, string applicationName,
                                    string endpoint, string headerjson,
                                    string requestType)
        {

            try
            {
                var plexusLog = new PlexusLogRequestModel
                {
                    AppDomainType = AppDomainType.Plexus,
                    AppName = applicationName,
                    CorrelationId = correlationId,
                    ClientIpAddress = _ipAddressProvider.GetClientIpAddress(),
                    ServerIpAddress = _ipAddressProvider.GetHostIpAddress(),
                    OrganisationAsId = organisationAsId,
                    Username = _tokenService.GetUsername(),
                    UserRoleId = (int)_tokenService.GetUserRole(),
                    SystemIdentifier = _tokenService.GetSystemIdentifier(),
                    ServiceEvent = "Read",
                    RequestType = requestType,
                    Resource = endpoint,
                    RequestBody = content,
                    RequestHeader = headerjson,
                    UserId="Todo:Add user id",
                    NhsNumber= "Todo:Add nhs number",
                    OrganisationOdsCode="Todo: Add ods code",
                    PracticeOdsCode = "Todo"
                };

                await _loggingTopicPublisher.PublishAsync(plexusLog, correlationId, Lchra.MessageBroker.Common.Messages.MessageType.LogMessage.PlexusLog);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
