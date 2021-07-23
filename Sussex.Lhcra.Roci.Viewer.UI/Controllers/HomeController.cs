using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Common.Domain.Audit.Models;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Roci.Viewer.DataServices;
using Sussex.Lhcra.Roci.Viewer.DataServices.Models;
using Sussex.Lhcra.Roci.Viewer.Domain.Models;
using Sussex.Lhcra.Roci.Viewer.UI.Configurations;
using Sussex.Lhcra.Roci.Viewer.UI.Extensions;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers;
using Sussex.Lhcra.Roci.Viewer.UI.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Sussex.Lhcra.Roci.Viewer.Services.Core;
using Sussex.Lhcra.Roci.Viewer.Domain.Interfaces;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{

    //[ServiceFilter(typeof(SessionTimeout))]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICertificateProvider _appSecretsProvider;
        private readonly ViewerAppSettingsConfiguration _viewerConfiguration;
        private readonly ISmspProxyDataService _smspProxyDataService;
        private readonly IRociGatewayDataService _rociGatewayDataService;
        private readonly IIpAddressProvider _ipAddressProvider;
        private readonly ITokenService _tokenService;
        private readonly IAuditLogTopicPublisher _auditLogTopicPublisher;

        public HomeController(
            ILogger<HomeController> logger,
            IOptions<ViewerAppSettingsConfiguration> configurationOption,
            ISmspProxyDataService smspProxyDataService,
            IRociGatewayDataService rociGatewayDataService,
            IIpAddressProvider ipAddressProvider,
            ITokenService tokenService,
            IConfiguration configuration, ICertificateProvider appSecretsProvider, IAuditLogTopicPublisher auditLogTopicPublisher)
        {
            _logger = logger;
            _viewerConfiguration = configurationOption.Value;
            _smspProxyDataService = smspProxyDataService;
            _rociGatewayDataService = rociGatewayDataService;
            _ipAddressProvider = ipAddressProvider;
            _tokenService = tokenService;
            _configuration = configuration;
            _appSecretsProvider = appSecretsProvider;
            _auditLogTopicPublisher = auditLogTopicPublisher;
        }

        protected bool IsProd => _configuration.GetValue<bool>("IsProd");
        protected string SmspIntEnvAsid => _configuration.GetValue<string>("SmspIntEnvAsid");

        

        [HttpGet]
        public async Task<IActionResult> Index(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }

            SaveModelToViewBag(nhsNumber, dob,
            organisationASID, organisationODScode, userId,
            userName, userRole, sessionId, correlationId,
            patientGivenName, patientFamilyName, patientPostCode,
            patientGender, patientPracticeOdsCode, patientAddress,practitionerNamePrefix,practitionerGivenName, practitionerFamilyName);

            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;
            organisationAsid = organisationASID ?? organisationAsid;

            var strSpineModel = "";

            try
            {
                strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);            
            }
            catch
            {
                return View("InvalidCertErrorPage");
            }

            var spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;
            //spineModel.GpPractice = new Smsp.Domain.Models.GpPracticeModel {  };


            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Summary);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Summary, patientGivenName, patientFamilyName,  patientPostCode,patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        private async Task<bool> LogAuditRecordModel(HttpRequest request, PatientCareRecordRequestDomainModel model, Guid correlationId, string section)
        {
            var auditLog = new AuditLogRequestModel(AppDomainType.Plexus, _ipAddressProvider.GetClientIpAddress(), _ipAddressProvider.GetHostIpAddress(), _tokenService.GetSystemIdentifier(), _viewerConfiguration.ApplicationName + $" --SECTION--(" + section + ")"
                                                    , GetAbsolutePath(Request), model.OrganisationAsId, model.PractitionerId, model.NhsNumber, _tokenService.GetUsername(), _tokenService.GetTokenString(), Guid.NewGuid().ToString()
                                                    , Guid.NewGuid().ToString(), correlationId, _tokenService.GetUserRole());
            try
            {
                await _auditLogTopicPublisher.PublishAsync(auditLog);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private DemographicsViewModel GetDemographicsDifferences(Patient patient, string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress, string dateOfBirth)
        {
            if (patient == null)
            {
                return null;
            }

            DemographicsViewModel model = new DemographicsViewModel();

            var givenNames = patient.Name.SelectMany(x => x.Given);
            var familyNames = patient.Name.SelectMany(x => x.Family);
            var otherNames = patient.Name.SelectMany(x => x.Suffix);
            var fullnames = patient.Name.Select(x => x.Text);
            var prefixes = patient.Name.SelectMany(x => x.Prefix);

            givenNames = givenNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
            familyNames = familyNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
            fullnames = fullnames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
            prefixes = prefixes.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());

            var localfamilyName = patientFamilyName;
            var localGivenName = patientGivenName;


            if (familyNames.Any() && !string.IsNullOrEmpty(localfamilyName))
            {
                var familyNameExists = familyNames.Contains(localfamilyName.Trim().ToUpper());

                if (!familyNameExists)
                {
                    model.FamilyNames = string.Join(",", familyNames);
                    model.LocalFamilyNames = localfamilyName;
                    model.DifferencesFound = true;
                }
            }

            if (givenNames.Any() && !string.IsNullOrEmpty(localGivenName))
            {
                var givenNameExists = givenNames.Contains(localGivenName.Trim().ToUpper());

                if (!givenNameExists)
                {
                    model.GivenNames = string.Join(",", givenNames);
                    model.LocalGivenNames = localGivenName;
                    model.DifferencesFound = true;
                }
            }

            
            if (patient.Address != null)
            {
                var postcodes = patient.Address.Where(x => !string.IsNullOrEmpty(x.PostalCode)).Select(x => x.PostalCode.Trim().ToUpper());
                var gpConnectAddresses = patient.Address.SelectMany(x => x.Line).ToList();
                var fullAddress = patient.Address.Select(x => x.Text).ToList();
                gpConnectAddresses.AddRange(fullAddress);
                gpConnectAddresses.ForEach(x => x = x.ToUpper());

                if(postcodes.Any())
                {
                    var postcodeExists = postcodes.Contains(patientPostCode.Trim().ToUpper());

                    if (!postcodeExists)
                    {
                        model.Postcode = string.Join(",", postcodes);
                        model.LocalPostcode = patientPostCode;
                        model.DifferencesFound = true;
                    }
                }

                if(gpConnectAddresses.Any())
                {
                    //var addressExists = gpConnectAddresses.Contains(patientAddress.Trim().ToUpper());

                    var addressExists = false;

                    foreach (var gpConnectAddress in gpConnectAddresses)
                    {
                        if(gpConnectAddress.ToUpper().Contains(patientAddress.Trim().ToUpper()))
                        {
                            addressExists = true;
                        }
                    }

                    if (!addressExists)
                    {
                        model.Addreses = string.Join(",", gpConnectAddresses);
                        model.LocalAddreses = patientAddress;
                        model.DifferencesFound = true;
                    }
                }
            }

            //var localeDob = dateOfBirth;

            //if (!patient.BirthDate.Equals(localeDob))
            //{
            //    model.DateOfBirth = patient.BirthDate;
            //    model.DifferencesFound = true;
            //}

            if (patient.Gender.HasValue)
            {
                var sameGender = patient.Gender.Value.ToString().ToUpper().Contains(patientGender);

                if (!sameGender)
                {
                    model.Gender = patient.Gender.Value.ToString();
                    model.LocalGender = patientGender;
                    model.DifferencesFound = true;
                }
            }
            

            if (patient.ManagingOrganization != null)
            {
                if(!string.IsNullOrEmpty(patient.ManagingOrganization.Display))
                {
                    if(patient.ManagingOrganization.Display.ToUpper() != patientPracticeOdsCode.ToUpper())
                    {
                        model.GPPracticeODSCode = patient.ManagingOrganization.Display;
                        model.LocalGPPracticeODSCode = patientPracticeOdsCode;
                        model.DifferencesFound = true;
                    }
                }
                else
                {
                    model.GPPracticeODSCode = "NULL";
                    model.LocalGPPracticeODSCode = patientPracticeOdsCode;
                    model.DifferencesFound = true;
                }
             
            }
            else
            {
                //model.GPPracticeODSCode = "NULL";
                //model.LocalGPPracticeODSCode = patientPracticeOdsCode;
                //model.DifferencesFound = true;
            }          

            return model;

        }

        [HttpGet]
        public async Task<IActionResult> Summary(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }



            SaveModelToViewBag(nhsNumber, dob,
                 organisationASID, organisationODScode, userId,
                 userName, userRole, sessionId, correlationId,
                 patientGivenName, patientFamilyName, patientPostCode,
                 patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

             var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

               spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }           

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Summary);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Summary, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }


        [HttpGet]
        public async Task<IActionResult> ProblemsAndIssues(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }

            SaveModelToViewBag(nhsNumber, dob,
                organisationASID, organisationODScode, userId,
                userName, userRole, sessionId, correlationId,
                patientGivenName, patientFamilyName, patientPostCode,
                patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.ProblemsAndIssues);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.ProblemsAndIssues, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.ProblemsAndIssues, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }



        [HttpGet]
        public async Task<IActionResult> Immunisations(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }

            SaveModelToViewBag(nhsNumber, dob,
             organisationASID, organisationODScode, userId,
             userName, userRole, sessionId, correlationId,
             patientGivenName, patientFamilyName, patientPostCode,
             patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Immunisations);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Immunisations, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Immunisations, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }


        [HttpGet]
        public async Task<IActionResult> Medication(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }

            SaveModelToViewBag(nhsNumber, dob,
               organisationASID, organisationODScode, userId,
               userName, userRole, sessionId, correlationId,
               patientGivenName, patientFamilyName, patientPostCode,
               patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Medication);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Medication, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Medication, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }


        [HttpGet]
        public async Task<IActionResult> Allergies(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }

            SaveModelToViewBag(nhsNumber, dob,
               organisationASID, organisationODScode, userId,
               userName, userRole, sessionId, correlationId,
               patientGivenName, patientFamilyName, patientPostCode,
               patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Allergies);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Allergies, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Allergies, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }


        [HttpGet]
        public async Task<IActionResult> Encounters(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }


            SaveModelToViewBag(nhsNumber, dob,
                        organisationASID, organisationODScode, userId,
                        userName, userRole, sessionId, correlationId,
                        patientGivenName, patientFamilyName, patientPostCode,
                        patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Encounters);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Encounters, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Encounters, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }


        [HttpGet]
        public async Task<IActionResult> Observations(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }


            SaveModelToViewBag(nhsNumber, dob,
               organisationASID, organisationODScode, userId,
               userName, userRole, sessionId, correlationId,
               patientGivenName, patientFamilyName, patientPostCode,
               patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Observations);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Observations, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Observations, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }


        [HttpGet]
        public async Task<IActionResult> Referrals(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }



            SaveModelToViewBag(nhsNumber, dob,
                organisationASID, organisationODScode, userId,
                userName, userRole, sessionId, correlationId,
                patientGivenName, patientFamilyName, patientPostCode,
                patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Referrals);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Referrals, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Referrals, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }

        [HttpGet]
        public async Task<IActionResult> Admin(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);
           
            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }


            SaveModelToViewBag(nhsNumber, dob,
                 organisationASID, organisationODScode, userId,
                 userName, userRole, sessionId, correlationId,
                 patientGivenName, patientFamilyName, patientPostCode,
                 patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Admin);
            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Admin, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Admin, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }

        [HttpGet]
        public async Task<IActionResult> Clinical(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress,
            string practitionerNamePrefix, string practitionerGivenName, string practitionerFamilyName)
        {
            UrlParemetersModel urlModel = new UrlParemetersModel();

            urlModel.AddNHSNumber(nhsNumber).AddDateOfBirth(dob).AddOrganisationASID(organisationASID)
                .AddOrganisationODScode(organisationODScode).AddUserId(userId).AddUserName(userName).AddUserRole(userRole)
                .AddSessionId(sessionId).AddCorrelationId(correlationId).AddPatientGivenName(patientGivenName).AddPatientFamilyName(patientFamilyName)
                .AddPatientPostCode(patientPostCode).AddPatientGender(patientGender).AddPatientPracticeOdsCode(patientPracticeOdsCode).AddPatientAddress(patientAddress)
                .AddPractitionerNamePrefix(practitionerNamePrefix).AddPractitionerGivenName(practitionerGivenName).AddPractitionerFamilyName(practitionerFamilyName);

            if (!urlModel.IsValid())
            {
                return View("InvalidModelErrorPage", urlModel);
            }

            SaveModelToViewBag(nhsNumber, dob,
                      organisationASID, organisationODScode, userId,
                      userName, userRole, sessionId, correlationId,
                      patientGivenName, patientFamilyName, patientPostCode,
                      patientGender, patientPracticeOdsCode, patientAddress, practitionerNamePrefix, practitionerGivenName, practitionerFamilyName);


            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;

            organisationAsid = organisationASID ?? organisationAsid;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                var strSpineModel = "";

                try
                {
                    strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{dob}", correlationId, organisationAsid);
                }
                catch
                {
                    return View("InvalidCertErrorPage");
                }

                spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            }

            if (spineModel == null)
            {
                return View("InvalidSpineModelErrorPage");
            }

            spineModel.OrganisationOdsCode = organisationODScode ?? Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = userId;
            spineModel.CorrelationId = correlationId;
            spineModel.Username = userName;
            spineModel.PractitionerNamePrefix = practitionerNamePrefix;
            spineModel.PractitionerGivenName = practitionerGivenName;
            spineModel.PractitionerFamilyName = practitionerFamilyName;

            SetPatientModelSession(spineModel);
            SetUrlParametersModelSession(urlModel);

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Clinical);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Clinical, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Clinical, patientGivenName, patientFamilyName, patientPostCode, patientGender, patientPracticeOdsCode, patientAddress);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);

        }
       
        
        private void SaveModelToViewBag(string nhsNumber, string dob,
            string organisationASID, string organisationODScode, string userId,
            string userName, string userRole, string sessionId, string correlationId,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress, string practitionerNamePrefix,
            string practitionerGivenName, string practitionerFamilyName)
        {
            ViewBag.Dob = dob;
            ViewBag.NhsNumber = nhsNumber;
            ViewBag.OrganisationASID = organisationASID;
            ViewBag.OrganisationODScode = organisationODScode;
            ViewBag.UserName = userName;
            ViewBag.UserId = userId;
            ViewBag.UserRole = userRole;
            ViewBag.SessionId = sessionId;
            ViewBag.CorrelationId = correlationId;
            ViewBag.PatientGivenName = patientGivenName;
            ViewBag.PatientFamilyName = patientFamilyName;
            ViewBag.PatientPostCode = patientPostCode;
            ViewBag.PatientGender = patientGender;
            ViewBag.PatientPracticeOdsCode = patientPracticeOdsCode;
            ViewBag.PatientAddress = patientAddress;
            ViewBag.PractitionerNamePrefix = practitionerNamePrefix;
            ViewBag.PractitionerGivenName = practitionerGivenName;
            ViewBag.PractitionerFamilyName = practitionerFamilyName;
        }
        public async Task<IActionResult> MentalHealthCrisisPlans(string dob, string nhsNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            ViewBag.Dob = dob;
            ViewBag.NhsNumber = nhsNumber;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                return RedirectToAction("Index");
            }

            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

            var patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.MentalHealthCrisisPlans, correlationId, spineModel.OrganisationAsId, spineModel);

            return View(Constants.MentalHealthCrisisPlans, patientCarePlanRecords);
        }

        public async Task<IActionResult> CommunityCarePlans(string dob, string nhsNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            ViewBag.Dob = dob;

            ViewBag.NhsNumber = nhsNumber;

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                return RedirectToAction("Index");
            }

            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

            var patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.CommunityCarePlans, correlationId, spineModel.OrganisationAsId, spineModel);

            return View(Constants.CommunityCarePlans, patientCarePlanRecords);
        }

        public void SetPatientModelSession(PatientCareRecordRequestDomainModel model)
        {
            if (HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName) == null)
            {
                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName, model);
            }
        }

        public void SetUrlParametersModelSession(UrlParemetersModel model)
        {
            if (HttpContext.Session.Get<UrlParemetersModel>(Constants.ViewerSessionUrlParametersKeyName) == null)
            {
                HttpContext.Session.Set<UrlParemetersModel>(Constants.ViewerSessionUrlParametersKeyName, model);
            }
        }

        public PatientCareRecordRequestDomainModel GetPatientModelSession()
        {
            return HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName);
        }

        private string GetAbsolutePath(HttpRequest request)
        {
            var absoluteUri = string.Concat(
                request.Scheme,
                "://",
                request.Host.ToUriComponent(),
                request.PathBase.ToUriComponent(),
                request.Path.ToUriComponent(),
                request.QueryString.ToUriComponent());

            return absoluteUri;

        }

        public async Task<JsonResult> GetDemographicDiv()
        {
            var s = HttpContext.Session.Get<string>(Constants.ViewerSessionDemographicDiv);
            return Json(new { content = s });
        }

        private async Task<ResourceViewModel> GetViewModel(string bundle, string dateOfBirth, string nhsNumber, string heading,
            string patientGivenName, string patientFamilyName, string patientPostCode,
            string patientGender, string patientPracticeOdsCode, string patientAddress)
        {
            try
            {

                var fjp = new FhirJsonParser();
                var gpBundle = fjp.Parse<Hl7.Fhir.Model.Bundle>(bundle);
                var vm = new ResourceViewModel();

                var compositions = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Composition).Cast<Composition>().ToList();

                var patient = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Patient).Cast<Patient>().FirstOrDefault();

                var title = "";
                title = patient.Name.Any() ? patient.Name.FirstOrDefault().PrefixElement.FirstOrDefault().ToString() : "";
                var sectionsDivs = compositions.SelectMany(x => x.Section.Select(y => y.Text.Div)).ToList();
                var div = sectionsDivs.FirstOrDefault();

                var dob = DateTime.Now;
                DateTime.TryParse(dateOfBirth, out dob);
                var age = dob.CalculateAge();

                vm.Div = div;
                var demographicsDiff = GetDemographicsDifferences(patient,patientGivenName, patientFamilyName, patientPostCode,patientGender,  patientPracticeOdsCode,  patientAddress, dateOfBirth);

                vm.DemographicsDiffDivModel = demographicsDiff;

                if (demographicsDiff != null && demographicsDiff.DifferencesFound)
                {
                    vm.DifferencesFound = 1;
                }
                    vm.Patient = patient;
                vm.Detail = "PLEXUS SUMMARY";
                vm.Heading = heading;
                vm.FormattedDateOfBirth = dateOfBirth;
                vm.NhsNumber = nhsNumber;
                vm.Age = age;
                vm.StrAge = $"{age}y";
                vm.Title = title;
                return vm;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> GetDemographicsDiff(Patient patient)
        {
            var spineModel = GetPatientModelSession();

            if (spineModel == null || patient == null)
            {
                return "";
            }

            DemographicsViewModel model = new DemographicsViewModel();

            var differencesFound = false;

            var givenNames = patient.Name.SelectMany(x => x.Given);
            var familyNames = patient.Name.SelectMany(x => x.Family);
            var otherNames = patient.Name.SelectMany(x => x.Suffix);
            var fullnames = patient.Name.Select(x => x.Text);
            var prefixes = patient.Name.SelectMany(x => x.Prefix);

            givenNames = givenNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
            familyNames = familyNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
            fullnames = fullnames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
            prefixes = prefixes.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());

            var spinePrefix = spineModel.Person.Prefix;
            var spinefamilyName = spineModel.Person.FamilyName;

            var spineGivenName1 = spineModel.Person.GivenName1;
            var spineGivenName2 = spineModel.Person.GivenName2;

            var spineGivennames = new List<string>();

            if (!string.IsNullOrEmpty(spineModel.Person.Prefix) && prefixes.Any())
            {
                if (!prefixes.Contains(spineModel.Person.Prefix.Trim().ToUpper()))
                {
                    model.Prefixes = spineModel.Person.Prefix.Trim().ToUpper();
                    differencesFound = true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(spineModel.Person.Prefix))
                {
                    model.Prefixes = String.Join(",", prefixes.ToList());
                    differencesFound = true;
                }
                else if (!prefixes.Any())
                {
                    model.Prefixes = spineModel.Person.Prefix;
                    differencesFound = true;
                }
            }

            if (!string.IsNullOrEmpty(spineGivenName1))
            {
                spineGivennames.Add(spineGivenName1);
            }

            if (!string.IsNullOrEmpty(spineGivenName2))
            {
                spineGivennames.Add(spineGivenName2);
            }


            if (spineGivennames.Any() || givenNames.Any())
            {
                if (!spineGivennames.Any())
                {
                    model.GivenNames = String.Join(", ", givenNames.ToList());
                    differencesFound = true;
                }
                else if (!givenNames.Any())
                {
                    model.GivenNames = String.Join(", ", spineGivennames.ToList());
                    differencesFound = true;
                }
                else
                {

                    var givennamesDiff = spineGivennames.Except(givenNames, StringComparer.OrdinalIgnoreCase);

                    if (givennamesDiff.Any())
                    {
                        model.GivenNames = String.Join(", ", givennamesDiff.ToList());
                        differencesFound = true;
                    }
                }
            }

            if (familyNames.Any() && !string.IsNullOrEmpty(spineModel.Person.FamilyName))
            {
                var familyNameExists = familyNames.Contains(spineModel.Person.FamilyName.Trim().ToUpper());

                if (familyNameExists)
                {
                    model.FamilyNames = spineModel.Person.FamilyName;
                    differencesFound = true;
                }
            }
            else
            {
                if (!familyNames.Any())
                {
                    model.FamilyNames = spineModel.Person.FamilyName;
                    differencesFound = true;
                }
                else if (string.IsNullOrEmpty(spineModel.Person.FamilyName))
                {
                    model.FamilyNames = spineModel.Person.FamilyName;
                    differencesFound = true;
                }
            }

            if (patient.Address != null && spineModel.Person.Address != null)
            {
                var postcodes = patient.Address.Where(x => !string.IsNullOrEmpty(x.PostalCode)).Select(x => x.PostalCode.Trim().ToUpper());
                var gpConnectAddresses = patient.Address.SelectMany(x => x.Line);

                var spinePostcode = spineModel.Person.Address.PostalCode;
                var address1 = spineModel.Person.Address.AddressLine1;
                var address2 = spineModel.Person.Address.AddressLine2;
                var address3 = spineModel.Person.Address.AddressLine3;
                var address4 = spineModel.Person.Address.AddressLine4;
                var address5 = spineModel.Person.Address.AddressLine5;

                var spineAddressList = new List<string>();

                if (!string.IsNullOrEmpty(address1))
                {
                    spineAddressList.Add(address1);
                }
                if (!string.IsNullOrEmpty(address2))
                {
                    spineAddressList.Add(address2);
                }
                if (!string.IsNullOrEmpty(address3))
                {
                    spineAddressList.Add(address3);
                }
                if (!string.IsNullOrEmpty(address4))
                {
                    spineAddressList.Add(address4);
                }
                if (!string.IsNullOrEmpty(address5))
                {
                    spineAddressList.Add(address5);
                }

                if (postcodes != null && postcodes.Any() && string.IsNullOrEmpty(spinePostcode))
                {
                    var postCodeExist = postcodes.Contains(spinePostcode.Trim().ToUpper());
                    if (!postCodeExist)
                    {
                        model.Postcode = spinePostcode;
                        differencesFound = true;
                    }
                }

                if (spineAddressList.Any() || gpConnectAddresses.Any())
                {
                    if (!spineAddressList.Any())
                    {
                        model.Addreses = String.Join(", ", gpConnectAddresses.ToList());
                        differencesFound = true;
                    }
                    else if (!gpConnectAddresses.Any())
                    {
                        model.Addreses = String.Join(", ", spineAddressList.ToList());
                        differencesFound = true;
                    }
                    else
                    {
                        var addressDiff = gpConnectAddresses.Except(spineAddressList, StringComparer.OrdinalIgnoreCase);

                        if (addressDiff.Any())
                        {
                            model.Addreses = String.Join(", ", addressDiff.ToList());
                            differencesFound = true;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(patient.BirthDate))
            {
                model.DateOfBirth = spineModel.Person.DateOfBirth.ToShortDateString();
                differencesFound = true;
            }
            else
            {
                var spineDob = spineModel.Person.DateOfBirth.ToShortDateString();

                if (!patient.BirthDate.Equals(spineDob))
                {
                    model.DateOfBirth = spineModel.Person.DateOfBirth.ToShortDateString();
                    differencesFound = true;
                }
            }

            spineModel.Person.Gender = 'F';

            if (patient.Gender.HasValue || !string.IsNullOrEmpty(spineModel.Person.Gender.ToString()))
            {
                var sameGender = patient.Gender.Value.ToString().ToUpper().Contains(spineModel.Person.Gender);

                if (!sameGender)
                {
                    model.Gender = spineModel.Person.Gender.ToString();
                    differencesFound = true;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(spineModel.Person.Gender.ToString()))
                {
                    model.Gender = spineModel.Person.Gender.ToString();
                    differencesFound = true;
                }

            }

            if (patient.ManagingOrganization != null && spineModel.GpPractice != null)
            {
                //Do Compare 
            }
            else
            {
                if (patient.ManagingOrganization == null)
                {
                    model.GPPracticeAddress = spineModel.GpPractice.Address;
                    model.GPPracticeODSCode = spineModel.GpPractice.OdsCode;
                    differencesFound = true;
                }
            }


            var s = "";

            if (differencesFound)
            {
                s = await this.RenderViewAsync("~/Views/Shared/_DemographicsDisplay.cshtml", model);
                HttpContext.Session.Set<string>(Constants.ViewerSessionDemographicDiv, s);

            }

            return s;

        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
