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
using Sussex.Lhcra.Roci.Viewer.Services;
using Microsoft.Identity.Client;
using Sussex.Lhcra.Roci.Viewer.Domain;

namespace Sussex.Lhcra.Roci.Viewer.UI.Controllers
{
    [ServiceFilter(typeof(SessionTimeout))]
    [Authorize()]
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
        private readonly IGraphProvider _graphProvider;

        public HomeController(
            ILogger<HomeController> logger,
            IOptions<ViewerAppSettingsConfiguration> configurationOption,
            ISmspProxyDataService smspProxyDataService,
            IRociGatewayDataService rociGatewayDataService,
            IIpAddressProvider ipAddressProvider,
            ITokenService tokenService,
            IConfiguration configuration, ICertificateProvider appSecretsProvider, IAuditLogTopicPublisher auditLogTopicPublisher, IGraphProvider graphProvider)
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
            _graphProvider = graphProvider;
        }

        protected bool IsProd => _configuration.GetValue<bool>("IsProd");
        protected string SmspIntEnvAsid => _configuration.GetValue<string>("SmspIntEnvAsid");


        public IActionResult Index()
        {
            var model = new ResourceViewModel
            {
                DateOfBirth = new DateTime(1927, 6, 19),
                NhsNumber = "9658218873"
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientData(string patientView)
        {   
            var guid = Guid.NewGuid();

            var correlationId = guid.ToString();

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                return RedirectToAction("Index");
            }

            //SetPatientModelSession(spineModel);

            await LogAuditRecordModel(Request, spineModel, guid, patientView);

            var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, patientView, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle || pBundle.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                pBundle.CorrelationId = correlationId;
                _logger.LogError(message: $"Not found error for patient {spineModel.NhsNumber} and correlation Id {spineModel.CorrelationId}", args: spineModel );
                return View("Error", pBundle);
            }

            var vm = GetViewModel(pBundle.StrBundle, spineModel.DateOfBirth, spineModel.NhsNumber, patientView, spineModel);

            if (null == vm)
            {
                pBundle.CorrelationId = correlationId;
                return View("Error", pBundle);
            }

            vm.ActiveView = patientView;

            vm.DifferencesFound = vm.ActiveView == Constants.Summary && vm.DifferencesFound == 1 ? 1 : 0;

            return View(Constants.All, vm);

        }

        public static string SerialisedMentalHealthCP = @"[{""NHSNumber"":""9990099782"",""PlanType"":null,""Category"":null,""Name"":null,""DateStart"":""2016-01-01T00:00:00"",""DateEnd"":null,""Aim"":""In the event of a crisis between 8:30am and 5pm on week days please contact the Assessment and Treatment Team on 01323 747222 and ask to speak to your lead practitioner. If they are not available at this time leave a message and it will be answered within the working day. Outside of the above hours, please contact the Mental Healthline on 0300 5000 101, this service is available from 5pm-9am Monday to Friday and 24/7/ at weekends and Bank Holidays.If you are feeling that you cannot keep yourself safe, present to A&E to be seen by Mental Health Liaison Service."",""ProviderName"":""SPFT""}]";
        public static string SerialisedCCP = @"[{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Community Nursing"",""Name"":""skin flap injury to back  1-2 units"",""DateStart"":""2015-02-03T14:42:32"",""DateEnd"":""2015-03-03T00:00:00"",""Aim"":""Curative"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Community Nursing"",""Name"":""?urine burns to groins 1-2 units"",""DateStart"":""2015-03-02T11:29:18"",""DateEnd"":""2015-03-03T00:00:00"",""Aim"":""Curative"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Community Nursing"",""Name"":""ESHT Blood Sampling"",""DateStart"":""2015-11-23T10:54:29"",""DateEnd"":""2016-01-15T00:00:00"",""Aim"":""Assessment"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Blood Pressure"",""Name"":""Check blood pressure"",""DateStart"":""2015-11-23T10:54:29"",""DateEnd"":""2016-01-15T00:00:00"",""Aim"":""Assessment"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Blood Sampling"",""DateStart"":""2016-11-02T13:37:52"",""DateEnd"":""2016-11-11T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Blood Sampling\nCSN PLEASE ATTEND"",""DateStart"":""2018-07-03T08:46:00"",""DateEnd"":""2018-11-03T00:00:00"",""Aim"":""Assessment"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""assess for pressure relieving equipment."",""DateStart"":""2019-06-11T08:55:12"",""DateEnd"":""2019-06-11T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Blood Sampling"",""DateStart"":""2019-06-12T12:47:16"",""DateEnd"":""2019-06-20T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Community Nursing"",""Name"":""breast wound dressing"",""DateStart"":""2016-03-14T12:49:43"",""DateEnd"":""2016-04-12T00:00:00"",""Aim"":""Curative"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Community Nursing"",""Name"":""pressure area care"",""DateStart"":""2016-03-15T11:00:00"",""DateEnd"":""2016-04-12T00:00:00"",""Aim"":""Curative"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""BP check"",""DateStart"":""2017-03-29T12:36:12"",""DateEnd"":""2017-04-17T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Blood Sampling"",""DateStart"":""2017-04-03T13:33:33"",""DateEnd"":""2017-04-12T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Ear syringing  4 units"",""DateStart"":""2017-08-14T14:09:54"",""DateEnd"":""2017-08-20T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""Lying / Sitting / Standing BP check check heart rate"",""DateStart"":""2018-03-12T16:20:38"",""DateEnd"":""2018-05-20T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Blood Sampling"",""DateStart"":""2018-03-13T11:11:03"",""DateEnd"":""2018-03-14T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Nursing Care Plan"",""DateStart"":""2018-04-25T12:42:42"",""DateEnd"":""2018-05-20T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Initial Nursing Assessment 2"",""DateStart"":""2020-04-17T09:24:07"",""DateEnd"":""2020-05-29T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT Pressure Ulcer to heel (2 units)"",""DateStart"":""2020-04-17T09:24:07"",""DateEnd"":""2020-05-29T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""ESHT Community Nursing"",""Name"":""ESHT  Primary  Prevention Plan (Purpose T)"",""DateStart"":""2020-04-28T10:00:00"",""DateEnd"":""2020-05-29T00:00:00"",""Aim"":""Unset"",""ProviderName"":""ESHT""},{""NHSNumber"":""4149277095"",""PlanType"":""Physical health"",""Category"":""Community Nursing"",""Name"":""sore grion assess 2-3 units"",""DateStart"":""2015-01-09T10:49:43"",""DateEnd"":""2015-01-22T00:00:00"",""Aim"":""Curative"",""ProviderName"":""ESHT""}]";


        public async Task<IActionResult> MentalHealthCrisisPlans(string patientView)
        {
            var correlationId = Guid.NewGuid().ToString();

            var spineModel = GetPatientModelSession();

            if (spineModel == null)
            {
                return RedirectToAction("Index");
            }

            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

            IEnumerable<PatientCarePlanRecord> patientCarePlanRecords = null;

            patientCarePlanRecords = await _rociGatewayDataService.GetCarePlanDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, patientView, correlationId, spineModel.OrganisationAsId, spineModel);

            if(patientView == Constants.CommunityCarePlans)
            {
                var scftpatientCarePlanRecords = patientCarePlanRecords.ToList();

                scftpatientCarePlanRecords.Add(new PatientCarePlanRecord 
                { 
                    Aim = "Continence Problems",
                    Category = "Priority 2 - Within 48 hours",
                    DateEnd = new DateTime(2019, 01, 19, 0, 0, 0),
                    DateStart = new DateTime(2019,01,19,0,0,0),
                    Name = "Community Nursing",
                    PlanType = "PCN Community Team Burgess Hill and Villages SCFT",
                    ProviderName = "SPFT",
                });

                patientCarePlanRecords = scftpatientCarePlanRecords.ToList();
            }


            var vm = new ResourceViewModel
            {
                NhsNumber = spineModel.NhsNumber,
                ActiveView = patientView,
                Plans = patientCarePlanRecords
            };

            return View(patientView, vm);

        }


        [HttpPost]
        public async Task<IActionResult> Summary(DateTime dateOfBirth, string nhsNumber)
        {        
            var guid = Guid.NewGuid();
            var correlationId = guid.ToString();
            var strDod = dateOfBirth.ToString("dd-MM-yyyy");
            ViewBag.Dob = strDod;
            ViewBag.NhsNumber = nhsNumber;

            PatientCareRecordRequestDomainModel spineModel = null;

            var organisationAsId = Constants.OrganisationAsId;//Pass on from user Credentials

            try
            {
                var strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{strDod}", correlationId, organisationAsId);
               
                if(strSpineModel.isValid)
                {
                    spineModel = JsonConvert.DeserializeObject<PatientCareRecordRequestDomainModel>(strSpineModel.SpineData);
                }
                else
                {
                    var errorModel = new PatientCareRecordBundleDomainViewModel();
                    errorModel.Message = strSpineModel.ErrorMessage;
                    errorModel.CorrelationId = correlationId;
                    _logger.LogError(message: $"Model error: ", args: errorModel);
                    return View("Error", errorModel);
                }           
           
                await FillUserDetailsFromAzureAsync(spineModel);

                spineModel.DateOfBirth = strDod;
                spineModel.CorrelationId = correlationId;


                SetPatientModelSession(spineModel, true);

                await LogAuditRecordModel(Request, spineModel, guid, Constants.Summary);

                var pBundle = await _rociGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.RociGatewayApiEndPoint, Constants.Summary, correlationId, spineModel.OrganisationAsId, spineModel);
                if (null == pBundle)
                {
                    _logger.LogError(message: $"Patient care record null error: ", args: spineModel);
                    return View("Error");
                }

                var vm = GetViewModel(pBundle.StrBundle, strDod, nhsNumber, Constants.Summary, spineModel);

                if (null == vm)
                {
                    _logger.LogError(message: $"View model empty error: NHS Number {nhsNumber}");
                    return View("Error", pBundle);
                }

                vm.ActiveView = Constants.Summary;

                return View(Constants.All, vm);
            }
            catch (InvalidCertificateException certificateException)
            {
                _logger.LogError($"A certificate exception has occured: { certificateException}");
                var errorModel = new PatientCareRecordBundleDomainViewModel();
                errorModel.Message = certificateException.Message;
                errorModel.CorrelationId = correlationId;
                return View("InvalidCertErrorPage", errorModel);
            }
            catch (MsalUiRequiredException msalException)
            {
                _logger.LogError($"An exception has occured: { msalException}");
                return RedirectToAction("SignOut", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception has occured: { ex}");
                return RedirectToAction("SignOut", "Account");
            }
        }

        private async Task FillUserDetailsFromAzureAsync(PatientCareRecordRequestDomainModel requestModel)
        {
            var plexusUser = await _graphProvider.GetLoggedInUserDetails(MandatoryFields.UserAzureProperties);

            if (!plexusUser.IsValid())
            {
                _logger.LogWarning($"Some of the mandatory details of the logged in user are missing in Azure", string.Join(",", plexusUser.MissingProperties()));
               // throw new MissingUserDetailsException($"Some of the mandatory details of the logged in user are missing in Azure {string.Join(",", plexusUser.MissingProperties())}");
            }
            
            requestModel.PractitionerId = plexusUser.UserId;
            requestModel.RequestorId = plexusUser.UserId;
            requestModel.Username = plexusUser.Username;
            requestModel.PractitionerNamePrefix = plexusUser.UsernamePrefix;
            requestModel.PractitionerGivenName = plexusUser.GivenName;
            requestModel.PractitionerFamilyName = plexusUser.FamilyName;
            requestModel.PractitionerRoleId = "UNK";
            requestModel.SdsUserId = "UNK";

            if (requestModel.GpPractice != null)
                requestModel.GpPractice.Name = plexusUser.OrganisationName ?? ""; //todo
            else
                requestModel.GpPractice = new Smsp.Domain.Models.GpPracticeModel() { Name = plexusUser.OrganisationName };   
            
            requestModel.OrganisationOdsCode = string.IsNullOrEmpty(plexusUser.OrganisationOdsCode) ? Constants.OrganisationOdsCode : plexusUser.OrganisationOdsCode;
           
            //uncomment once the users' details are ready in Azure
            // requestModel.OrganisationAsId = string.IsNullOrEmpty(plexusUser.OrganisationAsid)? Constants.OrganisationAsId : plexusUser.OrganisationAsid ;
            requestModel.OrganisationAsId = Constants.OrganisationAsId;
        }

        private void CheckForNull(PlexusUser plexusUser)
        {
            var missingProperties = plexusUser.GetType().GetProperties()
                    .Where(p => p.PropertyType == typeof(string) && string.IsNullOrEmpty((string)p.GetValue(plexusUser)) == true)
                    .Select(p => p.Name);

            if (missingProperties.Count() > 0)
            {
                _logger.LogError($"Some of the mandatory details of the logged in user are missing in Azure", string.Join(",", missingProperties));
                throw new MissingUserDetailsException($"Some of the mandatory details of the logged in user are missing in Azure {string.Join(",", missingProperties)}");
            }
        }

        public void SetPatientModelSession(PatientCareRecordRequestDomainModel model, bool clear = false)
        {
            if(clear)
                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName, null);

            if (HttpContext.Session.Get<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName) == null)
            {
                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>(Constants.ViewerSessionKeyName, model);
            }
        }

        private ResourceViewModel GetViewModel(string bundle, string dateOfBirth, string nhsNumber, string heading,
        PatientCareRecordRequestDomainModel spineModel)
        {
            
            try
            {

                var fjp = new FhirJsonParser();
                var gpBundle = fjp.Parse<Hl7.Fhir.Model.Bundle>(bundle);
                var vm = new ResourceViewModel();

                var compositions = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Composition).Cast<Composition>().ToList();

                var patient = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Patient).Cast<Patient>().FirstOrDefault();

                var organisation = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Organization).Cast<Organization>().FirstOrDefault();

                var title = "";
                title = patient.Name.Any() ? patient.Name.FirstOrDefault().PrefixElement.FirstOrDefault().ToString() : "";
                var sectionsDivs = compositions.SelectMany(x => x.Section.Select(y => y.Text.Div)).ToList();
                var div = sectionsDivs.FirstOrDefault();

                var dob = DateTime.Now;
                DateTime.TryParse(dateOfBirth, out dob);
                var age = dob.CalculateAge();

                vm.Div = div;

                DemographicsViewModel demographicsDiff = null;
                vm.DifferencesFound = 0;

                if (heading == Constants.Summary)
                {
                    demographicsDiff = GetDemographicsDifferences(patient, organisation, spineModel);

                    vm.DemographicsDiffDivModel = demographicsDiff;

                    if (demographicsDiff != null && demographicsDiff.DifferencesFound)
                    {
                        vm.DifferencesFound = 1;
                    }
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

        public JsonResult GetDemographicDiv()
        {
            var s = HttpContext.Session.Get<string>(Constants.ViewerSessionDemographicDiv);
            return Json(new { content = s });
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

        private DemographicsViewModel GetDemographicsDifferences(Patient patient, Organization organisation, PatientCareRecordRequestDomainModel spineModel)
        {
            if (!(patient != null && spineModel != null && spineModel.Person != null))
            {
                return null;
            }

            var person = spineModel.Person;

            string patientGivenName = person.GivenName1;
            string patientFamilyName = person.FamilyName;
            string patientPostCode = person.Address.PostalCode;
            string patientGender = person.Gender.ToString();
            string patientPracticeOdsCode = spineModel.GpPractice.OdsCode;
            string patientAddress = person.Address.AddressLine1;

            if(string.IsNullOrEmpty(patientAddress))
            {
                patientAddress = person.Address.AddressLine2;
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

            if (familyNames.Any() && !string.IsNullOrEmpty(patientFamilyName))
            {
                var familyNameExists = familyNames.Contains(patientFamilyName.Trim().ToUpper());

                if (!familyNameExists)
                {
                    model.FamilyNames = string.Join(",", familyNames);
                    model.LocalFamilyNames = patientFamilyName;
                    model.DifferencesFound = true;
                }
            }

            if (givenNames.Any() && !string.IsNullOrEmpty(patientGivenName))
            {
                var givenNameExists = givenNames.Contains(patientGivenName.Trim().ToUpper());

                if (!givenNameExists)
                {
                    model.GivenNames = string.Join(",", givenNames);
                    model.LocalGivenNames = patientGivenName;
                    model.DifferencesFound = true;
                }
            }


            if (patient.Address != null)
            {
                var postcodes = patient.Address.Where(x => !string.IsNullOrEmpty(x.PostalCode)).Select(x => x.PostalCode.Trim().ToUpper());
                var gpConnectAddresses = patient.Address.SelectMany(x => x.Line).ToList();
                gpConnectAddresses = gpConnectAddresses.Where(x => !string.IsNullOrEmpty(x)).ToList();
                var fullAddress = patient.Address.Where(x => !string.IsNullOrEmpty(x.Text)).Select(x => x.Text).ToList();
                gpConnectAddresses.AddRange(fullAddress);
                gpConnectAddresses.ForEach(x => x = x.ToUpper());

                if (postcodes.Any())
                {
                    var postcodeExists = postcodes.Contains(patientPostCode.Trim().ToUpper());

                    if (!postcodeExists)
                    {
                        model.Postcode = string.Join(",", postcodes);
                        model.LocalPostcode = patientPostCode;
                        model.DifferencesFound = true;
                    }
                }

                if (gpConnectAddresses.Any())
                {
                    var addressExists = false;

                    foreach (var gpConnectAddress in gpConnectAddresses)
                    {
                        if (gpConnectAddress.ToUpper().Contains(patientAddress.Trim().ToUpper()))
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

            if (organisation != null)
            {
                var odsCodeIdentifier = organisation.Identifier.FirstOrDefault();

                if (odsCodeIdentifier != null && odsCodeIdentifier.Value != null)
                {
                    if (odsCodeIdentifier.Value.ToUpper() != patientPracticeOdsCode.ToUpper())
                    {
                        //model.GPPracticeODSCode = patient.ManagingOrganization.Display;
                        //model.LocalGPPracticeODSCode = patientPracticeOdsCode;
                        //model.DifferencesFound = true;
                    }
                }
                else
                {
                    model.GPPracticeODSCode = "NULL";
                    model.LocalGPPracticeODSCode = patientPracticeOdsCode;
                    model.DifferencesFound = true;
                }

            }

            return model;

        }

    }
}
