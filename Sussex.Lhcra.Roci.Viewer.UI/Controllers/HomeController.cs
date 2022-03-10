using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Common.Domain.Audit.Models;
using Sussex.Lhcra.Common.Domain.Constants;
using Sussex.Lhcra.Plexus.Viewer.DataServices;
using Sussex.Lhcra.Plexus.Viewer.Domain;
using Sussex.Lhcra.Plexus.Viewer.Domain.Interfaces;
using Sussex.Lhcra.Plexus.Viewer.Domain.Models;
using Sussex.Lhcra.Plexus.Viewer.Services;
using Sussex.Lhcra.Plexus.Viewer.Services.Core;
using Sussex.Lhcra.Plexus.Viewer.UI.Configurations;
using Sussex.Lhcra.Plexus.Viewer.UI.Extensions;
using Sussex.Lhcra.Plexus.Viewer.UI.Helpers;
using Sussex.Lhcra.Plexus.Viewer.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sussex.Lhcra.Plexus.Viewer.UI.Controllers
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
        private readonly IPlexusGatewayDataService _plexusGatewayDataService;
        private readonly IIpAddressProvider _ipAddressProvider;
        private readonly ITokenService _tokenService;
        private readonly IAuditLogTopicPublisher _auditLogTopicPublisher;
        private readonly IGraphProvider _graphProvider;

        public HomeController(
            ILogger<HomeController> logger,
            IOptions<ViewerAppSettingsConfiguration> configurationOption,
            ISmspProxyDataService smspProxyDataService,
            IPlexusGatewayDataService rociGatewayDataService,
            IIpAddressProvider ipAddressProvider,
            ITokenService tokenService,
            IConfiguration configuration, ICertificateProvider appSecretsProvider, IAuditLogTopicPublisher auditLogTopicPublisher, IGraphProvider graphProvider)
        {
            _logger = logger;
            _viewerConfiguration = configurationOption.Value;
            _smspProxyDataService = smspProxyDataService;
            _plexusGatewayDataService = rociGatewayDataService;
            _ipAddressProvider = ipAddressProvider;
            _tokenService = tokenService;
            _configuration = configuration;
            _appSecretsProvider = appSecretsProvider;
            _auditLogTopicPublisher = auditLogTopicPublisher;
            _graphProvider = graphProvider;
        }

        protected bool IsProd => _configuration.GetValue<bool>("IsProd");
        protected string SmspIntEnvAsid => _configuration.GetValue<string>("SmspIntEnvAsid");


        public async Task<IActionResult> IndexAsync()
        {
            try
            {
                PlexusUser userDetailsFromAzure;
                var userFromSession = GetUserDetailsSession();

                if (userFromSession != null)
                {
                    userDetailsFromAzure = userFromSession;
                }
                else
                {
                    userDetailsFromAzure = await GetAzureUserDetails();

                    if (!userDetailsFromAzure.IsValid())
                    {
                        var err = $"Some of the mandatory details of the logged in user are missing in Azure : {string.Join(",", userDetailsFromAzure.MissingProperties())}";
                        _logger.LogError(err);
                        var errorModel = new PatientCareRecordBundleDomainViewModel() { Message = err };
                        return View("ExceptionPage", errorModel);
                    }

                    SetUserDetailsSession(userDetailsFromAzure);
                }

                var model = new ResourceViewModel
                {
                    DateOfBirth = new DateTime(1927, 6, 19),
                    NhsNumber = "9658218873"
                };

                return View(model);
            }
            catch (MsalUiRequiredException msalException)
            {
                _logger.LogError($"An exception has occured: { msalException}");
                return RedirectToAction("SignOut", "Account");
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && (ex.InnerException.Message.Contains("MsalUiRequiredException")))
                {
                    _logger.LogError($"An exception has occured: { ex.InnerException}");
                    return RedirectToAction("SignOut", "Account");
                }

                _logger.LogError($"An exception has occured: { ex}");
                var errorModel = new PatientCareRecordBundleDomainViewModel() { Message = ex.Message };
                return View("ExceptionPage", errorModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPatientData(string patientView, string nhsNumber)
        {
            var guid = Guid.NewGuid();

            var correlationId = guid.ToString();

            var spineModel = GetPatientModelSession(nhsNumber);

            if (spineModel == null)
            {
                return RedirectToAction("Index");
            }

            //SetPatientModelSession(spineModel);

            await LogAuditRecordModel(Request, spineModel, guid, patientView);

            var pBundle = await _plexusGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.PlexusGatewayApiEndPoint, patientView, correlationId, spineModel.OrganisationAsId, spineModel);

            if (null == pBundle || pBundle.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                pBundle.CorrelationId = correlationId;
                _logger.LogError(message: $"Not found error for patient {spineModel.NhsNumber} and correlation Id {spineModel.CorrelationId}", args: spineModel);
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

        public async Task<IActionResult> MentalHealthCrisisPlans(string patientView, string nhsNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            var spineModel = GetPatientModelSession(nhsNumber);

            if (spineModel == null)
            {
                return RedirectToAction("Index");
            }

            spineModel.OrganisationOdsCode = Constants.OrganisationOdsCode;

            IEnumerable<PatientCarePlanRecord> patientCarePlanRecords = null;

            if(patientView == Constants.CommunityCarePlans ||  _viewerConfiguration.FetchMentalHealthData)
                patientCarePlanRecords = await _plexusGatewayDataService.GetCarePlanDataContentAsync(_viewerConfiguration.ProxyEndpoints.PlexusGatewayApiEndPoint, patientView, correlationId, spineModel.OrganisationAsId, spineModel);

            var vm = new ResourceViewModel
            {
                NhsNumber = spineModel.NhsNumber,
                ActiveView = patientView,
                Plans = patientCarePlanRecords
            };

            return View(patientView, vm);
        }


        [HttpPost]
        public async Task<IActionResult> Summary(DateTime dateOfBirth, string nhsNumber, string UserId)
        {
            var guid = Guid.NewGuid();
            var correlationId = guid.ToString();
            var strDob = dateOfBirth.ToString("dd-MM-yyyy");
            nhsNumber = nhsNumber.Trim().Replace(" ", "");

            var validation = ValidateDobAndNhsNumber(dateOfBirth, nhsNumber);

            if (!validation.IsValid)
            {
                ViewBag.ErrorMessage = validation.ValidationMessage;
                return View("Index");
            }

            ViewBag.Dob = strDob;
            ViewBag.NhsNumber = nhsNumber;
            PlexusUser userDetailsFromAzure;
            try
            {
                var userFromSession = GetUserDetailsSession();

                if (userFromSession != null)
                {
                    userDetailsFromAzure = userFromSession;
                }
                else
                {
                    userDetailsFromAzure = await GetAzureUserDetails();

                    if (!userDetailsFromAzure.IsValid())
                    {
                        var err = $"Some of the mandatory details of the logged in user are missing in Azure : {string.Join(",", userDetailsFromAzure.MissingProperties())}";
                        _logger.LogError(err);
                        var errorModel = new PatientCareRecordBundleDomainViewModel() { Message = err };
                        return View("ExceptionPage", errorModel);
                    }
                    SetUserDetailsSession(userDetailsFromAzure);
                }

                string organisationAsId = userDetailsFromAzure.OrganisationAsid;

                PatientCareRecordRequestDomainModel spineModel = null;
                var strSpineModel = await _smspProxyDataService.GetDataContent($"Spine/{nhsNumber}/{strDob}", correlationId, organisationAsId);

                if (strSpineModel.isValid)
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

                MapUserDetails(spineModel, userDetailsFromAzure);

                spineModel.DateOfBirth = strDob;
                spineModel.CorrelationId = correlationId;

                SetPatientModelSession(spineModel, true);

                await LogAuditRecordModel(Request, spineModel, guid, Constants.Summary);

                var pBundle = await _plexusGatewayDataService.GetDataContentAsync(_viewerConfiguration.ProxyEndpoints.PlexusGatewayApiEndPoint, Constants.Summary, correlationId, spineModel.OrganisationAsId, spineModel);
                if (null == pBundle)
                {
                    _logger.LogError(message: $"Patient care record null error: ", args: spineModel);
                    return View("Error");
                }

                var vm = GetViewModel(pBundle.StrBundle, strDob, nhsNumber, Constants.Summary, spineModel);

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
                if (ex.InnerException != null && (ex.InnerException.Message.Contains("MsalUiRequiredException")))
                {
                    _logger.LogError($"An exception has occured: { ex.InnerException}");
                    return RedirectToAction("SignOut", "Account");
                }

                _logger.LogError($"An exception has occured: { ex}");
                var errorModel = new PatientCareRecordBundleDomainViewModel() { Message = ex.Message };
                return View("ExceptionPage", errorModel);
            }
        }

        private (bool IsValid, string ValidationMessage) ValidateDobAndNhsNumber(DateTime dob, string nhsNumber)
        {
            var dobResult = ValidateDob(dob);

            nhsNumber = nhsNumber.Trim().Replace(" ", "");
            var nhsNumberResult = ValidateNhsNumber(nhsNumber);

            var validationErr = new StringBuilder();
            if (!nhsNumberResult.IsValid)
            {
                validationErr.Append(nhsNumberResult.ValidationMessage);
            }

            if (!dobResult.IsValid)
            {
                validationErr.Append(dobResult.ValidationMessage);
            }

            return (IsValid: nhsNumberResult.IsValid && dobResult.IsValid, ValidationMessage: validationErr.ToString());
        }

        private (bool IsValid, string ValidationMessage) ValidateDob(DateTime dob)
        {
            var age = dob.AgeInDaysAndYears();
            if (!(age.ageInDays > 0 && age.ageInYears <= 120))
            {
                return (IsValid: false, ValidationMessage: "DOB is invalid! Age must be greater than 0 days and less than 120 years.");
            }

            return (IsValid: true, ValidationMessage: string.Empty);
        }

        private static (bool IsValid, StringBuilder ValidationMessage) ValidateNhsNumber(string validatedNhsNumber)
        {
            var valid = false;
            var validationMessage = new StringBuilder();
            validationMessage.Append("NHS number entered invalid! ");

            if (validatedNhsNumber.Length == 10)
                valid = true;
            else
            {
                validationMessage.Append("NHS number must be exactly 10 digits without any spaces or special characters. ");
            }
            var regex = new Regex(@"^[0-9]+$");
            var isRegexMatch = regex.IsMatch(validatedNhsNumber);
            valid = valid && isRegexMatch;

            if (!isRegexMatch)
            {
                validationMessage.Append("Only numbers 0-9 allowed, no special characters allowed. ");
            }
            return (IsValid: valid, ValidationMessage: validationMessage);
        }

        private void MapUserDetails(PatientCareRecordRequestDomainModel requestModel, PlexusUser plexusUser)
        {
            requestModel.PractitionerId = plexusUser.UserId;
            requestModel.RequestorId = plexusUser.UserId;
            requestModel.Username = plexusUser.Username;
            requestModel.PractitionerNamePrefix = plexusUser.UsernamePrefix;
            requestModel.PractitionerGivenName = plexusUser.GivenName;
            requestModel.PractitionerFamilyName = plexusUser.FamilyName;
            requestModel.PractitionerRoleId = "UNK";
            requestModel.SdsUserId = "UNK";

            if (requestModel.GpPractice != null)
                requestModel.GpPractice.Name = plexusUser.OrganisationName;
            else
                requestModel.GpPractice = new Smsp.Domain.Models.GpPracticeModel() { Name = plexusUser.OrganisationName };

            requestModel.OrganisationOdsCode = plexusUser.OrganisationOdsCode;
            requestModel.OrganisationAsId = plexusUser.OrganisationAsid;
#if DEBUG
            requestModel.OrganisationOdsCode = Constants.OrganisationOdsCode ?? plexusUser.OrganisationOdsCode;
            requestModel.OrganisationAsId = Constants.OrganisationAsId ?? plexusUser.OrganisationAsid;
#endif
        }


        private async Task<PlexusUser> GetAzureUserDetails()
        {
            var plexusUser = await _graphProvider.GetLoggedInUserDetails(MandatoryFields.UserAzureProperties);
#if DEBUG
            plexusUser.OrganisationAsid = Constants.OrganisationAsId;
#endif
            return plexusUser;
        }

        public void SetPatientModelSession(PatientCareRecordRequestDomainModel model, bool clear = false)
        {
            if (clear)
                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>($"{Constants.ViewerSessionKeyName}.{model.NhsNumber}", null);

            if (HttpContext.Session.Get<PatientCareRecordRequestDomainModel>($"{Constants.ViewerSessionKeyName}.{model.NhsNumber}") == null)
            {
                HttpContext.Session.Set<PatientCareRecordRequestDomainModel>($"{Constants.ViewerSessionKeyName}.{model.NhsNumber}", model);
            }
        }


        public void SetUserDetailsSession(PlexusUser user, bool clear = true)
        {
            if (clear)
                HttpContext.Session.Set<PlexusUser>($"{Constants.UserDetailsSessionKeyName}", null);

            if (HttpContext.Session.Get<PlexusUser>($"{Constants.UserDetailsSessionKeyName}") == null)
            {
                HttpContext.Session.Set<PlexusUser>($"{Constants.UserDetailsSessionKeyName}", user);
            }
        }

        public PlexusUser GetUserDetailsSession()
        {
            return HttpContext.Session.Get<PlexusUser>($"{Constants.UserDetailsSessionKeyName}");
        }

        private ResourceViewModel GetViewModel(string bundle, string dateOfBirth, string nhsNumber, string heading,
        PatientCareRecordRequestDomainModel spineModel)
        {

            try
            {

                var fjp = new FhirJsonParser();
                var gpBundle = fjp.Parse<Bundle>(bundle);
                var vm = new ResourceViewModel();

                var compositions = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Composition).Cast<Composition>().ToList();

                var patient = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Patient).Cast<Patient>().FirstOrDefault();

                var organisation = gpBundle.GetResources().Where(x => x.ResourceType == ResourceType.Organization).Cast<Organization>().FirstOrDefault();

                var title = "";
                title = patient.Name.Any() ? patient.Name.FirstOrDefault().PrefixElement.FirstOrDefault().ToString() : "";
                var sectionsDivs = compositions.SelectMany(x => x.Section.Select(y => y.Text.Div)).ToList();
                var div = sectionsDivs.FirstOrDefault();

                var age = dateOfBirth.CalculateAge();

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


        public PatientCareRecordRequestDomainModel GetPatientModelSession(string nhsNumber)
        {
            return HttpContext.Session.Get<PatientCareRecordRequestDomainModel>($"{Constants.ViewerSessionKeyName}.{nhsNumber}");
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

            if (string.IsNullOrEmpty(patientAddress))
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
