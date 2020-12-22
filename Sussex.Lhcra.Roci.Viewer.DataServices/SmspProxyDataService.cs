﻿using Newtonsoft.Json;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Common.Domain.Logging.Models;
using Sussex.Lhcra.Common.Domain.Logging.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Roci.Viewer.DataServices
{
    public class SmspProxyDataService : ISmspProxyDataService
    {
        private const string ServiceName = "Spine Proxy";

        private readonly ILoggingDataService _loggingDataService;
        private readonly HttpClient _httpClient;

        public SmspProxyDataService(ILoggingDataService loggingDataService, HttpClient httpClient)
        {
            _loggingDataService = loggingDataService;
            _httpClient = httpClient;
        }

        public async Task<string> GetDataContent(string url, Guid correlationId)
        {
            await LogRequest(new HttpRequestMessage(), url, url, correlationId, ServiceName, _httpClient.BaseAddress.ToString());

            var httpResponse = await _httpClient.GetAsync(url);
            var returnData = await httpResponse.Content.ReadAsStringAsync();

            await LogResponse(
                url, 
                returnData, 
                correlationId, 
                ServiceName,
                _httpClient.BaseAddress.ToString(), 
                httpResponse, 
                (int)httpResponse.StatusCode);

            return returnData;
        }

        private async Task<bool> LogRequest(
            HttpRequestMessage request, 
            string organisationAsId, 
            string content, 
            Guid correlationId, 
            string applicationName,
            string endpoint)
        {
            var logRecord = new LogRecordRequestModel
            {
                AppName = applicationName,
                CorrelationId = correlationId,
                OrganisationAsId = organisationAsId,
                RequestMethod = LogConstants.RequestType.HttpPost,
                MessageType = LogConstants.MessageType.Request,
                Endpoint = endpoint,
                MessageBody = content,
                Exception = string.Empty,
                MessageHeader = JsonConvert.SerializeObject(request.Headers)
            };

            try
            {
                await _loggingDataService.LogRecordAsync(logRecord);
            }
            catch
            {
                return false;
            }

            return true;
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
