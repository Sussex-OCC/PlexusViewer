//using Hl7.Fhir.Model;
//using Hl7.Fhir.Serialization;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json;
//using Sussex.Lhcra.Common.AzureADServices.Interfaces;
//using Sussex.Lhcra.Common.ClientServices.Interfaces;
//using Sussex.Lhcra.Common.Domain.Audit.Models;
//using Sussex.Lhcra.Common.Domain.Constants;
//using Sussex.Lhcra.Roci.Viewer.DataServices;
//using Sussex.Lhcra.Roci.Viewer.DataServices.Models;
//using Sussex.Lhcra.Roci.Viewer.Domain.Models;
//using Sussex.Lhcra.Roci.Viewer.UI.Configurations;
//using Sussex.Lhcra.Roci.Viewer.UI.Extensions;
//using Sussex.Lhcra.Roci.Viewer.UI.Helpers;
//using Sussex.Lhcra.Roci.Viewer.UI.Models;
//using System;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using System.Collections.Generic;
//using Sussex.Lhcra.Roci.Viewer.Services.Core;

//namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
//{

//    //[ServiceFilter(typeof(SessionTimeout))]
//    public class HomeController : Controller
//    {
//        private readonly ILogger<HomeController> _logger;
//        private readonly IConfiguration _configuration;
//        private readonly IAppSecretsProvider _appSecretsProvider;
//        private readonly ViewerAppSettingsConfiguration _viewerConfiguration;
//        private readonly ISmspProxyDataService _smspProxyDataService;
//        private readonly IRociGatewayDataService _rociGatewayDataService;
//        private readonly IIpAddressProvider _ipAddressProvider;
//        private readonly ITokenService _tokenService;
//        private readonly LoggingServiceADSetting _loggingAdSettings;
//        private readonly AuditServiceADSetting _auditAdSettings;
//        private readonly IAuditLogTopicPublisher _auditLogTopicPublisher;

//        public HomeController(
//            ILogger<HomeController> logger,
//            IOptions<ViewerAppSettingsConfiguration> configurationOption,
//            ISmspProxyDataService smspProxyDataService,
//            IRociGatewayDataService rociGatewayDataService,
//            IAuditLogTopicPublisher auditLogTopicPublisher,
//            IIpAddressProvider ipAddressProvider,
//             ITokenService tokenService,
//             IOptions<LoggingServiceADSetting> loggingServiceOption,
//             IOptions<AuditServiceADSetting> auditServiceOption,
//            IConfiguration configuration, IAppSecretsProvider appSecretsProvider)
//        {
//            _logger = logger;
//            _viewerConfiguration = configurationOption.Value;
//            _smspProxyDataService = smspProxyDataService;
//            _rociGatewayDataService = rociGatewayDataService;
//            _auditLogTopicPublisher = auditLogTopicPublisher;
//            _ipAddressProvider = ipAddressProvider;
//            _tokenService = tokenService;
//            _loggingAdSettings = loggingServiceOption.Value;
//            _auditAdSettings = auditServiceOption.Value;
//            _configuration = configuration;
//            _appSecretsProvider = appSecretsProvider;
//        }

//        protected bool IsProd => _configuration.GetValue<bool>("IsProd");
//        protected string SmspIntEnvAsid => _configuration.GetValue<string>("SmspIntEnvAsid");


//        public IActionResult Index()
//        {
//            var vm = new ResourceViewModel
//            {
//                DateOfBirth = new DateTime(1927, 6, 19),
//                NhsNumber = "9658218873"
//            };

//            return View(vm);

//        }



//        [HttpPost]
//        public async Task<IActionResult> Summary(DateTime dateOfBirth, string nhsNumber)
//        {
//            var organisationAsid = IsProd ? _viewerConfiguration.OrganisationAsId : SmspIntEnvAsid;
//            var guid = Guid.NewGuid();
//            var correlationId = guid.ToString();
//            var strDod = dateOfBirth.ToString("dd-MM-yyyy");
//            ViewBag.Dob = strDod;
//            ViewBag.NhsNumber = nhsNumber;

//            var strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{strDod}", correlationId, organisationAsid);

//            var spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel);

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;
//            spineModel.OrganisationAsId = organisationAsid;
//            spineModel.PractitionerId = correlationId;
//            spineModel.Username = "PLEXUSVIEWER";

//            SetPatientModelSession(spineModel);

//            await LogAuditRecordModel(Request, spineModel, guid, Constants.Summary);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, organisationAsid, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dateOfBirth.ToString("dd-MMM-yyyy"), nhsNumber, Constants.Summary);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);

//        }


//        [HttpGet]
//        public async Task<IActionResult> Summary(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Summary);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Summary);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> ProblemsAndIssues(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();

//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.ProblemsAndIssues);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.ProblemsAndIssues, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.ProblemsAndIssues);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Immunisations(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Immunisations);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Immunisations, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Immunisations);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Investigations(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Investigations);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Investigations, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Investigations);

//            if (null == vm)
//            {
//                pBundle.CorrelationId = correlationId;
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Medication(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Medication);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Medication, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Medication);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Allergies(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Allergies);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Allergies, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Allergies);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Encounters(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Encounters);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Encounters, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Encounters);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Observations(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Observations);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Observations, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Observations);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Referrals(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Referrals);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Referrals, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Referrals);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> Clinical(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();
//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), _tokenService.GetUserRole().ToString());

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, _tokenService.GetUserRole().ToString(), correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, _tokenService.GetUserRole().ToString());

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public async Task<IActionResult> MentalHealthCrisisPlans(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();

//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.MentalHealthCrisisPlans);

//            var patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.MentalHealthCrisisPlans, correlationId, spineModel.OrganisationAsId, spineModel);

//            return View(Constants.MentalHealthCrisisPlans, patientCarePlanRecords);
//        }


//        public async Task<IActionResult> CommunityCarePlans(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();

//            ViewBag.Dob = dob;

//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.CommunityCarePlans);

//            var patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.CommunityCarePlans, correlationId, spineModel.OrganisationAsId, spineModel);

//            return View(Constants.CommunityCarePlans, patientCarePlanRecords);
//        }

//        public async Task<IActionResult> Admin(string dob, string nhsNumber)
//        {
//            var correlationId = Guid.NewGuid().ToString();

//            ViewBag.Dob = dob;
//            ViewBag.NhsNumber = nhsNumber;

//            var spineModel = GetPatientModelSession();

//            if (spineModel == null)
//            {
//                return RedirectToAction("Index");
//            }

//            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

//            await LogAuditRecordModel(Request, spineModel, new Guid(correlationId), Constants.Admin);

//            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Admin, correlationId, spineModel.OrganisationAsId, spineModel);

//            if (null == pBundle)
//            {
//                return View("Error");
//            }

//            var vm = await GetViewModel(pBundle.StrBundle, dob, nhsNumber, Constants.Admin);

//            if (null == vm)
//            {
//                return View("Error", pBundle);
//            }

//            return View(Constants.All, vm);
//        }

//        public void SetPatientModelSession(PatientCareRecordRequestDomainModel model)
//        {

//            if (HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName) == null)
//            {
//                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName, model);

//            }
//        }

//        public PatientCareRecordRequestDomainModel GetPatientModelSession()
//        {
//            return HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName);
//        }

//        private async Task<bool> LogAuditRecordModel(HttpRequest request, PatientCareRecordRequestDomainModel model, Guid correlationId, string section)
//        {
//            var auditLog = new AuditLogRequestModel(AppDomainType.Plexus, _ipAddressProvider.GetClientIpAddress(), _ipAddressProvider.GetHostIpAddress(), _tokenService.GetSystemIdentifier(), _viewerConfiguration.ApplicationName + $" --SECTION--(" + section + ")"
//                                                    , GetAbsolutePath(Request), model.OrganisationAsId, model.PractitionerId, model.NhsNumber, _tokenService.GetUsername(), _tokenService.GetTokenString(), Guid.NewGuid().ToString()
//                                                    , Guid.NewGuid().ToString(), correlationId, _tokenService.GetUserRole());
//            try
//            {
//                await _auditLogTopicPublisher.PublishAsync(auditLog);
//            }
//            catch
//            {
//                return false;
//            }

//            return true;
//        }

//        private string GetAbsolutePath(HttpRequest request)
//        {
//            var absoluteUri = string.Concat(
//                request.Scheme,
//                "://",
//                request.Host.ToUriComponent(),
//                request.PathBase.ToUriComponent(),
//                request.Path.ToUriComponent(),
//                request.QueryString.ToUriComponent());

//            return absoluteUri;

//        }


//        public async Task<JsonResult> GetDemographicDiv()
//        {
//            var s = HttpContext.Session.Get<string>(Constants.ViewerSessionDemographicDiv);
//            return Json(new { content = s });
//        }

//        private async Task<ResourceViewModel> GetViewModel(string bundle, string dateOfBirth, string nhsNumber, string heading)
//        {
//            try
//            {

//                var fjp = new FhirJsonParser();
//                var gpBundle = fjp.Parse<Hl7.Fhir.Model.Bundle>(bundle);
//                var vm = new ResourceViewModel();

//                var compositions = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Composition).Cast<Composition>().ToList();

//                var patient = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Patient).Cast<Patient>().FirstOrDefault();

//                var title = "";
//                title = patient.Name.Any() ? patient.Name.FirstOrDefault().PrefixElement.FirstOrDefault().ToString() : "";
//                var sectionsDivs = compositions.SelectMany(x => x.Section.Select(y => y.Text.Div)).ToList();
//                var div = sectionsDivs.FirstOrDefault();

//                var dob = DateTime.Now;
//                DateTime.TryParse(dateOfBirth, out dob);
//                var age = dob.CalculateAge();

//                vm.Div = div;
//                var demographicsDiff = await GetDemographicsDiff(patient);

//                vm.DemographicsDiffDiv = demographicsDiff;
//                vm.Patient = patient;
//                vm.Detail = "PLEXUS SUMMARY";
//                vm.Heading = heading;
//                vm.FormattedDateOfBirth = dateOfBirth;
//                vm.NhsNumber = nhsNumber;
//                vm.Age = age;
//                vm.StrAge = $"{age}y";
//                vm.Title = title;
//                return vm;
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        private async Task<string> GetDemographicsDiff(Patient patient)
//        {
//            var spineModel = GetPatientModelSession();

//            if (spineModel == null || patient == null)
//            {
//                return "";
//            }

//            DemographicsViewModel model = new DemographicsViewModel();

//            var differencesFound = false;

//            var givenNames = patient.Name.SelectMany(x => x.Given);
//            var familyNames = patient.Name.SelectMany(x => x.Family);
//            var otherNames = patient.Name.SelectMany(x => x.Suffix);
//            var fullnames = patient.Name.Select(x => x.Text);
//            var prefixes = patient.Name.SelectMany(x => x.Prefix);

//            givenNames = givenNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
//            familyNames = familyNames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
//            fullnames = fullnames.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());
//            prefixes = prefixes.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.ToUpper());

//            var spinePrefix = spineModel.Person.Prefix;
//            var spinefamilyName = spineModel.Person.FamilyName;

//            var spineGivenName1 = spineModel.Person.GivenName1;
//            var spineGivenName2 = spineModel.Person.GivenName2;

//            var spineGivennames = new List<string>();

//            if (!string.IsNullOrEmpty(spineModel.Person.Prefix) && prefixes.Any())
//            {
//                if (!prefixes.Contains(spineModel.Person.Prefix.Trim().ToUpper()))
//                {
//                    model.Prefixes = spineModel.Person.Prefix.Trim().ToUpper();
//                    differencesFound = true;
//                }
//            }
//            else
//            {
//                if (string.IsNullOrEmpty(spineModel.Person.Prefix))
//                {
//                    model.Prefixes = String.Join(",", prefixes.ToList());
//                    differencesFound = true;
//                }
//                else if (!prefixes.Any())
//                {
//                    model.Prefixes = spineModel.Person.Prefix;
//                    differencesFound = true;
//                }
//            }

//            if (!string.IsNullOrEmpty(spineGivenName1))
//            {
//                spineGivennames.Add(spineGivenName1);
//            }

//            if (!string.IsNullOrEmpty(spineGivenName2))
//            {
//                spineGivennames.Add(spineGivenName2);
//            }

//            //spineGivennames.Add("Andrew");

//            if (spineGivennames.Any() || givenNames.Any())
//            {
//                if (!spineGivennames.Any())
//                {
//                    model.GivenNames = String.Join(", ", givenNames.ToList());
//                    differencesFound = true;
//                }
//                else if (!givenNames.Any())
//                {
//                    model.GivenNames = String.Join(", ", spineGivennames.ToList());
//                    differencesFound = true;
//                }
//                else
//                {

//                    var givennamesDiff = spineGivennames.Except(givenNames, StringComparer.OrdinalIgnoreCase);

//                    if (givennamesDiff.Any())
//                    {
//                        model.GivenNames = String.Join(", ", givennamesDiff.ToList());
//                        differencesFound = true;
//                    }
//                }
//            }

//            if (familyNames.Any() && !string.IsNullOrEmpty(spineModel.Person.FamilyName))
//            {
//                var familyNameExists = familyNames.Contains(spineModel.Person.FamilyName.Trim().ToUpper());

//                if (familyNameExists)
//                {
//                    model.FamilyNames = spineModel.Person.FamilyName;
//                    differencesFound = true;
//                }
//            }
//            else
//            {
//                if (!familyNames.Any())
//                {
//                    model.FamilyNames = spineModel.Person.FamilyName;
//                    differencesFound = true;
//                }
//                else if (string.IsNullOrEmpty(spineModel.Person.FamilyName))
//                {
//                    model.FamilyNames = spineModel.Person.FamilyName;
//                    differencesFound = true;
//                }
//            }

//            if (patient.Address != null && spineModel.Person.Address != null)
//            {
//                var postcodes = patient.Address.Where(x => !string.IsNullOrEmpty(x.PostalCode)).Select(x => x.PostalCode.Trim().ToUpper());
//                var gpConnectAddresses = patient.Address.SelectMany(x => x.Line);

//                var spinePostcode = spineModel.Person.Address.PostalCode;
//                var address1 = spineModel.Person.Address.AddressLine1;
//                var address2 = spineModel.Person.Address.AddressLine2;
//                var address3 = spineModel.Person.Address.AddressLine3;
//                var address4 = spineModel.Person.Address.AddressLine4;
//                var address5 = spineModel.Person.Address.AddressLine5;

//                var spineAddressList = new List<string>();

//                if (!string.IsNullOrEmpty(address1))
//                {
//                    spineAddressList.Add(address1);
//                }
//                if (!string.IsNullOrEmpty(address2))
//                {
//                    spineAddressList.Add(address2);
//                }
//                if (!string.IsNullOrEmpty(address3))
//                {
//                    spineAddressList.Add(address3);
//                }
//                if (!string.IsNullOrEmpty(address4))
//                {
//                    spineAddressList.Add(address4);
//                }
//                if (!string.IsNullOrEmpty(address5))
//                {
//                    spineAddressList.Add(address5);
//                }

//                if (postcodes != null && postcodes.Any() && string.IsNullOrEmpty(spinePostcode))
//                {
//                    var postCodeExist = postcodes.Contains(spinePostcode.Trim().ToUpper());
//                    if (!postCodeExist)
//                    {
//                        model.Postcode = spinePostcode;
//                        differencesFound = true;
//                    }
//                }

//                if (spineAddressList.Any() || gpConnectAddresses.Any())
//                {
//                    if (!spineAddressList.Any())
//                    {
//                        model.Addreses = String.Join(", ", gpConnectAddresses.ToList());
//                        differencesFound = true;
//                    }
//                    else if (!gpConnectAddresses.Any())
//                    {
//                        model.Addreses = String.Join(", ", spineAddressList.ToList());
//                        differencesFound = true;
//                    }
//                    else
//                    {
//                        var addressDiff = gpConnectAddresses.Except(spineAddressList, StringComparer.OrdinalIgnoreCase);

//                        if (addressDiff.Any())
//                        {
//                            model.Addreses = String.Join(", ", addressDiff.ToList());
//                            differencesFound = true;
//                        }
//                    }
//                }
//            }

//            if (string.IsNullOrEmpty(patient.BirthDate))
//            {
//                model.DateOfBirth = spineModel.Person.DateOfBirth.ToShortDateString();
//                differencesFound = true;
//            }
//            else
//            {
//                var spineDob = spineModel.Person.DateOfBirth.ToShortDateString();

//                if (!patient.BirthDate.Equals(spineDob))
//                {
//                    model.DateOfBirth = spineModel.Person.DateOfBirth.ToShortDateString();
//                    differencesFound = true;
//                }
//            }

//            spineModel.Person.Gender = 'F';

//            if (patient.Gender.HasValue || !string.IsNullOrEmpty(spineModel.Person.Gender.ToString()))
//            {
//                var sameGender = patient.Gender.Value.ToString().ToUpper().Contains(spineModel.Person.Gender);

//                if (!sameGender)
//                {
//                    model.Gender = spineModel.Person.Gender.ToString();
//                    differencesFound = true;
//                }
//            }
//            else
//            {
//                if (string.IsNullOrEmpty(spineModel.Person.Gender.ToString()))
//                {
//                    model.Gender = spineModel.Person.Gender.ToString();
//                    differencesFound = true;
//                }

//            }

//            if (patient.ManagingOrganization != null && spineModel.GpPractice != null)
//            {
//                //Do Compare 
//            }
//            else
//            {
//                if (patient.ManagingOrganization == null)
//                {
//                    model.GPPracticeAddress = spineModel.GpPractice.Address;
//                    model.GPPracticeODSCode = spineModel.GpPractice.OdsCode;
//                    differencesFound = true;
//                }
//            }


//            var s = "";

//            if (differencesFound)
//            {
//                s = await this.RenderViewAsync("~/Views/Shared/_DemographicsDisplay.cshtml", model);
//                HttpContext.Session.Set<string>(Constants.ViewerSessionDemographicDiv, s);

//            }

//            return s;

//        }

//        public IActionResult Privacy()
//        {
//            return View();
//        }

//        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//        public IActionResult Error()
//        {
//            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//        }

//    }
//}
