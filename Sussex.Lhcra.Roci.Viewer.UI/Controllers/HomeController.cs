﻿using Hl7.Fhir.Model;
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

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ViewerAppSettingsConfiguration _configuration;

        private readonly ISmspProxyDataService _smspProxyDataService;
        private readonly IRociGatewayDataService _rociGatewayDataService;
        private readonly IIpAddressProvider _ipAddressProvider;
        private readonly ITokenService _tokenService;
        private readonly LoggingServiceADSetting _loggingAdSettings;
        private readonly AuditServiceADSetting _auditAdSettings;
        private readonly IAuditLogTopicPublisher _auditLogTopicPublisher;

        public HomeController(
            ILogger<HomeController> logger,
            IOptions<ViewerAppSettingsConfiguration> configurationOption,
            ISmspProxyDataService smspProxyDataService,
            IRociGatewayDataService rociGatewayDataService,
            IAuditLogTopicPublisher auditLogTopicPublisher,
            IIpAddressProvider ipAddressProvider,
             ITokenService tokenService,
             IOptions<LoggingServiceADSetting> loggingServiceOption,
             IOptions<AuditServiceADSetting> auditServiceOption)
        {
            _logger = logger;
            _configuration = configurationOption.Value;
            _smspProxyDataService = smspProxyDataService;
            _rociGatewayDataService = rociGatewayDataService;
            _auditLogTopicPublisher = auditLogTopicPublisher;
            _ipAddressProvider = ipAddressProvider;
            _tokenService = tokenService;
            _loggingAdSettings = loggingServiceOption.Value;
            _auditAdSettings = auditServiceOption.Value;
        }

        public IActionResult Index()
        {
            var vm = new ResourceViewModel
            {
                DateOfBirth = new DateTime(1927, 6, 19),
                NhsNumber = "9658218873"
            };

            return View(vm);
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

        [HttpPost]
        public async Task<IActionResult> Summary(DateTime dateOfBirth, string nhsNumber)
        {
            var organisationAsid = _configuration.OrganisationAsId;
            var guid = Guid.NewGuid();
            var correlationId = guid.ToString();
            var strDod = dateOfBirth.ToString("dd-MM-yyyy");
            ViewBag.Dob = strDod;
            ViewBag.NhsNumber = nhsNumber;

            var strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{strDod}", correlationId, organisationAsid);

            var spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);
            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;
            spineModel.OrganisationAsId = organisationAsid;
            spineModel.PractitionerId = correlationId;
            spineModel.Username = "PLEXUSVIEWER";

            SetPatientModelSession(spineModel);

            await LogAuditRecordModel(Request, spineModel, guid, Constants.Summary);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, organisationAsid, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dateOfBirth.ToString("dd-MMM-yyyy"), nhsNumber, Constants.Summary);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        [HttpGet]
        public async Task<IActionResult> Summary(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Summary);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Summary);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> ProblemsAndIssues(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.ProblemsAndIssues);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.ProblemsAndIssues, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.ProblemsAndIssues);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Immunisations(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Immunisations);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Immunisations, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Immunisations);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Investigations(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Investigations);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Investigations, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Investigations);

            if (null == vm)
            {
                pBundle.CorrelationId = correlationId;
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Medication(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Medication);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Medication, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Medication);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Allergies(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Allergies);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Allergies, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Allergies);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Encounters(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Encounters);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Encounters, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Encounters);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Observations(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Observations);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Observations, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Observations);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Referrals(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Referrals);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Referrals, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Referrals);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public async Task<IActionResult> Clinical(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Clinical);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Clinical, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Clinical);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.MentalHealthCrisisPlans);

            var patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.MentalHealthCrisisPlans, correlationId, spineModel.OrganisationAsId, spineModel);

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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.CommunityCarePlans);

            var patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.CommunityCarePlans, correlationId, spineModel.OrganisationAsId, spineModel);

            return View(Constants.CommunityCarePlans, patientCarePlanRecords);
        }

        public async Task<IActionResult> Admin(string dob, string nhsNumber)
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

            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Admin);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_configuration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Admin, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle)
            {
                return View("Error");
            }

            var vm = GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Admin);

            if (null == vm)
            {
                return View("Error", pBundle);
            }

            return View(Constants.All, vm);
        }

        public void SetPatientModelSession(PatientCareRecordRequestDomainModel model)
        {
            if (HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName) == null)
            {
                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName, model);
            }
        }

        public PatientCareRecordRequestDomainModel GetPatientModelSession()
        {
            return HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName);
        }

        private async Task<bool> LogAuditRecordModel(HttpRequest request, PatientCareRecordRequestDomainModel model, Guid correlationId, string section)
        {
            var auditLog = new AuditLogRequestModel(AppDomainType.Plexus, _ipAddressProvider.GetClientIpAddress(), _ipAddressProvider.GetHostIpAddress(), "PLEXUS VIewer System Identifier", _configuration.ApplicationName + $" --SECTION--(" + section + ")"
                                                    , GetAbsolutePath(Request), model.OrganisationAsId, model.PractitionerId, model.NhsNumber, _tokenService.GetUsername(), _tokenService.GetTokenString(), Guid.NewGuid().ToString()
                                                    , Guid.NewGuid().ToString(), correlationId, RoleIdType.Clinical);
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

        private static ResourceViewModel GetViewModel(string bundle, string dateOfBirth, string nhsNumber, string heading)
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
                vm.Patient = patient;
                vm.Detail = "PLEXUS Summary";
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
    }
}
