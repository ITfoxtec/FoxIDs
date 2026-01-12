using Blazored.Toast.Services;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FoxIDs.Client.Logic.Modules
{
    public class NemLoginUpPartyLogic
    {
        private const string NemLoginExtensionsNamespace = "https://data.gov.dk/eid/saml/extensions";
        private const string NemLoginAppSwitchAndroidProfileName = "android";
        private const string NemLoginAppSwitchIosProfileName = "ios";
        private const string NemLoginAppSwitchAndroidProfileDisplayName = "Android app-switch";
        private const string NemLoginAppSwitchIosProfileDisplayName = "iOS app-switch";
        private const string NemLoginAppSwitchPlatformAndroid = "Android";
        private const string NemLoginAppSwitchPlatformIos = "iOS";
        private const string NemLoginCprTransformPrefix = "nl_cpr_";
        private const string NemLoginCprTransformOpenExtendedUiName = NemLoginCprTransformPrefix + "opn";
        private const string NemLoginCprTransformQueryExternalUserName = NemLoginCprTransformPrefix + "qry";
        private const string NemLoginCprTransformRemoveJwtName = NemLoginCprTransformPrefix + "rmv";
        private const string NemLoginCprTransformOpenUiNoMatchName = NemLoginCprTransformPrefix + "nmt";
        private const string NemLoginCprTransformGuardName = NemLoginCprTransformPrefix + "grd";
        private const string NemLoginCprSubjectUuidPattern = ".*/eid/person/uuid/.*";
        private const string NemLoginCprSubjectUuidWithEmptyCprPattern = ".*/eid/person/uuid/.*\\|$";
        private static readonly string NemLoginCprGuardClaimType = $"{Constants.ClaimTransformClaimTypes.Namespace}nemlogin_cpr_guard";
        private const string NemLoginCprExtendedUiName = "cpr_match";
        private const string NemLoginOiosaml400LoaPrefix = "https://data.gov.dk/concept/core/loa/";
        private const string NemLoginOiosaml303LoaPrefix = "https://data.gov.dk/concept/core/nsis/loa/";
        private static readonly IReadOnlyList<string> NemLoginLoaLevels = ["Low", "Substantial", "High"];
        private static readonly IReadOnlyList<NemLoginSectors> NemLoginPrivateSectors = [NemLoginSectors.PrivateOiosaml400, NemLoginSectors.PrivateOiosaml303];
        private static readonly IReadOnlyList<NemLoginSectors> NemLoginOiosaml400Sectors = [NemLoginSectors.PublicOiosaml400, NemLoginSectors.PrivateOiosaml400];

        private sealed class NemLoginAppSwitchProfileDefinition
        {
            public NemLoginAppSwitchProfileDefinition(string name, string displayName, string platform)
            {
                Name = name;
                DisplayName = displayName;
                Platform = platform;
            }

            public string Name { get; }

            public string DisplayName { get; }

            public string Platform { get; }
        }

        public sealed class NemLoginAttributeProfileOption
        {
            public NemLoginAttributeProfileOption(string displayName, string id)
            {
                DisplayName = displayName;
                Id = id;
            }

            public string DisplayName { get; }

            public string Id { get; }
        }

        public sealed class NemLoginAuthnContextOption
        {
            public NemLoginAuthnContextOption(string displayName, string id)
            {
                DisplayName = displayName;
                Id = id;
            }

            public string DisplayName { get; }

            public string Id { get; }
        }

        private static readonly IReadOnlyList<NemLoginAttributeProfileOption> NemLoginAttributeProfileOptions = new List<NemLoginAttributeProfileOption>
        {
            new("DK Person", "https://data.gov.dk/eid/Person/DK"),
            new("DK Person without CPR", "https://data.gov.dk/eid/Person/DK/WithoutCPR"),
            new("DK Person (anonymized)", "https://data.gov.dk/eid/Person/DK/Anonymous"),
            new("DK Professional", "https://data.gov.dk/eid/Professional/DK"),
            new("DK Professional (anonymized)", "https://data.gov.dk/eid/Professional/DK/Anonymous"),
            new("eIDAS person", "https://data.gov.dk/eid/Person/EU"),
            new("eIDAS person (anonymized)", "https://data.gov.dk/eid/Person/EU/Anonymous"),
            new("eIDAS legal person", "https://data.gov.dk/eid/LegalPerson/EU"),
            new("eIDAS professional", "https://data.gov.dk/eid/Professional/EU"),
        };

        private static readonly IReadOnlyList<NemLoginAuthnContextOption> NemLoginOiosaml303IdTypeOptions = new List<NemLoginAuthnContextOption>
        {
            new("Person", "https://data.gov.dk/eid/Person"),
            new("Professional", "https://data.gov.dk/eid/Professional")
        };

        private static readonly IReadOnlyList<NemLoginAuthnContextOption> NemLoginOiosaml303CredentialTypeOptions = new List<NemLoginAuthnContextOption>
        {
            new("NemID key card", "https://nemlogin.dk/internal/credential/type/nemidkeycard"),
            new("NemID key file", "https://nemlogin.dk/internal/credential/type/nemidkeyfile"),
            new("MitID", "https://nemlogin.dk/internal/credential/type/mitid"),
            new("Local", "https://nemlogin.dk/internal/credential/type/local"),
            new("Test", "https://nemlogin.dk/internal/credential/type/test")
        };

        private static readonly HashSet<string> NemLoginOiosaml303AuthnContextValues =
            new(NemLoginOiosaml303IdTypeOptions.Select(option => option.Id)
                .Concat(NemLoginOiosaml303CredentialTypeOptions.Select(option => option.Id)),
                StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyList<NemLoginAppSwitchProfileDefinition> NemLoginAppSwitchProfileDefinitions = new List<NemLoginAppSwitchProfileDefinition>
        {
            new(NemLoginAppSwitchAndroidProfileName, NemLoginAppSwitchAndroidProfileDisplayName, NemLoginAppSwitchPlatformAndroid),
            new(NemLoginAppSwitchIosProfileName, NemLoginAppSwitchIosProfileDisplayName, NemLoginAppSwitchPlatformIos),
        };

        private readonly ClientSettings clientSettings;
        private readonly TrackService trackService;
        private readonly HelpersService helpersService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IToastService toastService;
        private Task<CertificateAndPassword> testCertificateTask;
        private Task<JwkWithCertificateInfo> testCertificateKeyTask;

        public NemLoginUpPartyLogic(
            ClientSettings clientSettings,
            TrackService trackService,
            HelpersService helpersService,
            IHttpClientFactory httpClientFactory,
            IToastService toastService)
        {
            this.clientSettings = clientSettings;
            this.trackService = trackService;
            this.helpersService = helpersService;
            this.httpClientFactory = httpClientFactory;
            this.toastService = toastService;
        }

        public IReadOnlyList<NemLoginAttributeProfileOption> AttributeProfileOptions => NemLoginAttributeProfileOptions;

        public IReadOnlyList<NemLoginAuthnContextOption> Oiosaml303IdTypeOptions => NemLoginOiosaml303IdTypeOptions;

        public IReadOnlyList<NemLoginAuthnContextOption> Oiosaml303CredentialTypeOptions => NemLoginOiosaml303CredentialTypeOptions;

        public bool IsPrivateSector(NemLoginSectors sector)
        {
            return NemLoginPrivateSectors.Contains(sector);
        }

        public bool IsOiosaml400Sector(NemLoginSectors sector)
        {
            return NemLoginOiosaml400Sectors.Contains(sector);
        }

        public bool IsOiosaml303Sector(NemLoginSectors sector)
        {
            return IsOiosaml303SectorInternal(sector);
        }

        private static bool IsPrivateSectorInternal(NemLoginSectors sector)
        {
            return NemLoginPrivateSectors.Contains(sector);
        }

        private static bool IsOiosaml400SectorInternal(NemLoginSectors sector)
        {
            return NemLoginOiosaml400Sectors.Contains(sector);
        }

        private static bool IsOiosaml303SectorInternal(NemLoginSectors sector)
        {
            return sector == NemLoginSectors.PublicOiosaml303 || sector == NemLoginSectors.PrivateOiosaml303;
        }

        public void EnsureNemLoginModule(SamlUpPartyViewModel model)
        {
            model.Modules ??= new SamlUpPartyModulesViewModel();
            model.Modules.NemLogin ??= new SamlUpPartyNemLoginModuleViewModel
            {
                Environment = NemLoginEnvironments.IntegrationTest,
                Sector = NemLoginSectors.PublicOiosaml303
            };

            if (!Enum.IsDefined(typeof(NemLoginEnvironments), model.Modules.NemLogin.Environment))
            {
                model.Modules.NemLogin.Environment = NemLoginEnvironments.IntegrationTest;
            }
            if (!Enum.IsDefined(typeof(NemLoginSectors), model.Modules.NemLogin.Sector))
            {
                model.Modules.NemLogin.Sector = NemLoginSectors.PublicOiosaml303;
            }

            if (!IsPrivateSectorInternal(model.Modules.NemLogin.Sector))
            {
                model.Modules.NemLogin.RequestCpr = false;
                model.Modules.NemLogin.SaveCprOnExternalUsers = false;
            }
            else if (!model.Modules.NemLogin.RequestCpr)
            {
                model.Modules.NemLogin.SaveCprOnExternalUsers = false;
            }
        }

        public void InitializeNemLoginTemplateModel(SamlUpPartyViewModel model)
        {
            EnsureNemLoginModule(model);
            EnsureNemLoginAuthnContextDefaults(model);
            var isOiosaml400 = IsOiosaml400SectorInternal(model.Modules.NemLogin.Sector);
            LoadNemLoginAuthnRequestExtensionsXml(model, isOiosaml400);

            if (isOiosaml400 && (model.Modules.NemLogin.NemLoginRequestedAttributeProfiles == null || model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Count == 0))
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = GetNemLoginDefaultAttributeProfileIds().ToList();
            }
            else if (!isOiosaml400)
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = new List<string>();
            }

            if (model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled || model.Modules.NemLogin.NemLoginAppSwitchIosEnabled)
            {
                UpdateNemLoginAppSwitchProfiles(model, isOiosaml400 ? model.Modules.NemLogin.NemLoginRequestedAttributeProfiles : null);
            }
        }

        public void ApplyNemLoginCreateDefaults(SamlUpPartyViewModel model)
        {
            EnsureNemLoginModule(model);

            model.IsManual = false;
            model.MetadataUpdateRate = 172800;
            model.MetadataUrl = GetNemLoginMetadataUrl(model.Modules.NemLogin.Environment, model.Modules.NemLogin.Sector);

            model.PartyBindingPattern = PartyBindingPatterns.Dot;
            model.SignatureAlgorithm = Saml2SecurityAlgorithms.RsaSha256Signature;
            model.SignAuthnRequest = true;
            model.DisableLoginHint = true;

            if (model.Modules.NemLogin.Environment == NemLoginEnvironments.Production)
            {
                model.CertificateValidationMode = X509CertificateValidationMode.ChainTrust;
                model.RevocationMode = X509RevocationMode.Online;
            }

            model.MetadataIncludeEncryptionCertificates = true;
            model.MetadataNameIdFormats = new List<string> { "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent" };

            if (IsOiosaml400SectorInternal(model.Modules.NemLogin.Sector))
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = GetNemLoginDefaultAttributeProfileIds().ToList();
            }
            else
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = new List<string>();
            }
            model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled = false;
            model.Modules.NemLogin.NemLoginAppSwitchAndroidReturnUrl = null;
            model.Modules.NemLogin.NemLoginAppSwitchIosEnabled = false;
            model.Modules.NemLogin.NemLoginAppSwitchIosReturnUrl = null;

            ApplyNemLoginProfileDefaults(model);
            EnsureNemLoginAuthnContextDefaults(model);
            UpdateNemLoginAuthnRequestExtensionsXml(model);

            ApplyNemLoginCprFlow(model);
            if (model.LinkExternalUser != null)
            {
                model.LinkExternalUser.ExternalUserLifetime = 0;
            }
        }

        public bool PrepareNemLoginBeforeSave(SamlUpPartyViewModel model)
        {
            EnsureNemLoginModule(model);

            var isValid = true;
            model.Modules.NemLogin.MetadataContactPersonsError = null;
            if (model.Modules.NemLogin.Environment == NemLoginEnvironments.Production && !HasCompleteNemLoginContactPerson(model.MetadataContactPersons))
            {
                model.Modules.NemLogin.MetadataContactPersonsError = "At least one metadata contact person must include company and email address for NemLog-in.";
                isValid = false;
            }

            model.Modules.NemLogin.NemLoginTrackCertificateError = null;
            if (model.Modules.NemLogin.NemLoginTrackCertificateInfo == null)
            {
                model.Modules.NemLogin.NemLoginTrackCertificateError = $"NemLog-in requires a {(model.Modules.NemLogin.Environment == NemLoginEnvironments.Production ? "production" : "test")} OCES3 certificate.";
                isValid = false;
            }

            if(!isValid)
            {
                return false;
            }

            model.IsManual = false;
            if (model.MetadataUrl.IsNullOrWhiteSpace())
            {
                model.MetadataUrl = GetNemLoginMetadataUrl(model.Modules.NemLogin.Environment, model.Modules.NemLogin.Sector);
            }

            model.PartyBindingPattern = PartyBindingPatterns.Dot;
            model.SignatureAlgorithm = Saml2SecurityAlgorithms.RsaSha256Signature;
            model.SignAuthnRequest = true;
            model.DisableLoginHint = true;

            ApplyNemLoginProfileDefaults(model);
            EnsureNemLoginAuthnContextDefaults(model);
            UpdateNemLoginAuthnRequestExtensionsXml(model);

            ApplyNemLoginCprFlow(model);
            return true;
        }

        public void HandleEnvironmentChanged(SamlUpPartyViewModel model, NemLoginEnvironments environment)
        {
            EnsureNemLoginModule(model);
            model.Modules.NemLogin.Environment = environment;
            model.MetadataUrl = GetNemLoginMetadataUrl(environment, model.Modules.NemLogin.Sector);

            if (environment == NemLoginEnvironments.Production)
            {
                model.CertificateValidationMode = X509CertificateValidationMode.None;
                model.RevocationMode = X509RevocationMode.Online;
            }
            else
            {
                model.CertificateValidationMode = X509CertificateValidationMode.None;
                model.RevocationMode = X509RevocationMode.NoCheck;
            }

            ApplyNemLoginCprFlow(model);
        }

        public void HandleMinimumLoaChanged(SamlUpPartyViewModel model, string loa)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);
            model.Modules.NemLogin.NemLoginMinimumLoa = loa.IsNullOrWhiteSpace() ? null : loa;
            if (model.Modules.NemLogin.NemLoginMinimumLoa.IsNullOrWhiteSpace() && model.AuthnContextClassReferences?.Count > 0)
            {
                model.AuthnContextClassReferences.RemoveAll(IsNemLoginLoaValue);
            }
            EnsureNemLoginAuthnContextDefaults(model);
        }

        public void HandleOiosaml303IdTypeChanged(SamlUpPartyViewModel model, string idType, bool isEnabled)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);
            if (!IsOiosaml303SectorInternal(model.Modules.NemLogin.Sector))
            {
                return;
            }

            UpdateNemLoginAuthnContextValue(model, idType, isEnabled);
        }

        public void HandleOiosaml303CredentialTypeChanged(SamlUpPartyViewModel model, string credentialType, bool isEnabled)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);
            if (!IsOiosaml303SectorInternal(model.Modules.NemLogin.Sector))
            {
                return;
            }

            UpdateNemLoginAuthnContextValue(model, credentialType, isEnabled);
        }

        public void HandleRequestedAttributeProfileChanged(SamlUpPartyViewModel model, string profileId, bool isEnabled)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);

            if (!IsOiosaml400SectorInternal(model.Modules.NemLogin.Sector))
            {
                return;
            }

            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles ??= new List<string>();

            if (isEnabled)
            {
                if (!model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Any(p => p.Equals(profileId, StringComparison.OrdinalIgnoreCase)))
                {
                    model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Add(profileId);
                }
            }
            else
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.RemoveAll(p => p.Equals(profileId, StringComparison.OrdinalIgnoreCase));
            }

            ApplyNemLoginAttributeProfileDefaults(model);
            UpdateNemLoginAuthnRequestExtensionsXml(model);
        }

        public void HandleAuthnRequestExtensionsChanged(SamlUpPartyViewModel model)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);

            UpdateNemLoginAuthnRequestExtensionsXml(model);
        }

        public void HandleSectorChanged(SamlUpPartyViewModel model, NemLoginSectors sector)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);
            model.Modules.NemLogin.Sector = sector;
            model.MetadataUrl = GetNemLoginMetadataUrl(model.Modules.NemLogin.Environment, sector);

            if (IsPrivateSectorInternal(sector))
            {
                model.Modules.NemLogin.RequestCpr = true;
                model.Modules.NemLogin.SaveCprOnExternalUsers = true;
            }
            else
            {
                model.Modules.NemLogin.RequestCpr = false;
                model.Modules.NemLogin.SaveCprOnExternalUsers = false;
            }

            ApplyNemLoginProfileDefaults(model);
            EnsureNemLoginAuthnContextDefaults(model);
            UpdateNemLoginAuthnRequestExtensionsXml(model);
            ApplyNemLoginCprFlow(model);
        }

        public void HandleCprFlowChanged(SamlUpPartyViewModel model)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            ApplyNemLoginCprFlow(model, true);
        }
        public async Task UpdateNemLoginEnvironmentAsync(SamlUpPartyViewModel model)
        {
            EnsureNemLoginModule(model);

            try
            {
                var logSettings = await trackService.GetTrackLogSettingAsync() ?? new LogSettings();
                logSettings.LogInfoTrace = true;
                logSettings.LogClaimTrace = true;
                logSettings.LogMessageTrace = true;
                await trackService.SaveTrackLogSettingAsync(logSettings);

                await trackService.UpdateTrackKeyTypeAsync(new TrackKey { Type = TrackKeyTypes.Contained });

                if (model.Modules.NemLogin.Environment == NemLoginEnvironments.IntegrationTest)
                {
                    if (!model.Modules.NemLogin.NemLoginTrackCertificateBase64Url.IsNullOrWhiteSpace())
                    {
                        await UploadTrackPrimaryCertificateAsync(new CertificateAndPassword
                        {
                            EncodeCertificate = model.Modules.NemLogin.NemLoginTrackCertificateBase64Url,
                            Password = model.Modules.NemLogin.NemLoginTrackCertificatePassword
                        });

                        model.Modules.NemLogin.NemLoginTrackCertificateBase64Url = null;
                        model.Modules.NemLogin.NemLoginTrackCertificatePassword = null;
                    }
                    else
                    {
                        var defaultCertificateKey = await GetNemLoginTestCertificateKeyAsync();
                        await UploadTrackPrimaryCertificateAsync(defaultCertificateKey);
                    }
                }
                else
                {
                    if (!model.Modules.NemLogin.NemLoginTrackCertificateBase64Url.IsNullOrWhiteSpace())
                    {
                        if (model.Modules.NemLogin.NemLoginTrackCertificatePassword.IsNullOrWhiteSpace())
                        {
                            toastService.ShowError("NemLog-in production certificate password is required.");
                            return;
                        }

                        await UploadTrackPrimaryCertificateAsync(new CertificateAndPassword
                        {
                            EncodeCertificate = model.Modules.NemLogin.NemLoginTrackCertificateBase64Url,
                            Password = model.Modules.NemLogin.NemLoginTrackCertificatePassword
                        });

                        model.Modules.NemLogin.NemLoginTrackCertificateBase64Url = null;
                        model.Modules.NemLogin.NemLoginTrackCertificatePassword = null;
                    }

                    var keys = await trackService.GetTrackKeyContainedAsync();
                    if (keys?.PrimaryKey == null)
                    {
                        toastService.ShowError("NemLog-in requires a production OCES3 certificate. Upload it on the Certificates tab for this environment.");
                    }
                }

                toastService.ShowSuccess("NemLog-in updated the environment (certificate and log settings). Consider using a separate environment for NemLog-in.");
            }
            catch (Exception ex)
            {
                toastService.ShowError($"NemLog-in was unable to update the environment. {ex.Message}");
            }
        }

        private string GetNemLoginMetadataUrl(NemLoginEnvironments environment, NemLoginSectors sector)
        {
            var nemLoginAssets = clientSettings?.ModuleAssets?.NemLogin;
            if (nemLoginAssets == null)
            {
                throw new InvalidOperationException("NemLog-in asset URLs are not loaded.");
            }

            string metadataUrl;
            if (IsOiosaml400SectorInternal(sector))
            {
                metadataUrl = environment == NemLoginEnvironments.Production
                    ? nemLoginAssets.MetadataProductionOiosaml400Url
                    : nemLoginAssets.MetadataIntegrationTestOiosaml400Url;
            }
            else
            {
                metadataUrl = environment == NemLoginEnvironments.Production
                    ? nemLoginAssets.MetadataProductionOiosaml303Url
                    : nemLoginAssets.MetadataIntegrationTestOiosaml303Url;
            }

            if (metadataUrl.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"NemLog-in metadata URL is not configured for environment '{environment}' and sector '{sector}'.");
            }

            return metadataUrl;
        }

        private static List<string> GetNemLoginDefaultClaims()
        {
            return new List<string>
            {
                "https://data.gov.dk/concept/core/nsis/loa",
                "https://data.gov.dk/model/core/eid/cprNumber",
                "https://data.gov.dk/model/core/eid/email",
                "https://data.gov.dk/model/core/eid/firstName",
                "https://data.gov.dk/model/core/eid/lastName",
                "https://data.gov.dk/model/core/eid/professional/cvr",
                "https://data.gov.dk/model/core/eid/professional/orgName",
                "https://data.gov.dk/model/core/eid/professional/uuid/persistent",
                "https://data.gov.dk/model/core/specVersion"
            };
        }

        private static IReadOnlyCollection<string> GetNemLoginDefaultAttributeProfileIds()
        {
            return new[]
            {
                "https://data.gov.dk/eid/Person/DK",
                "https://data.gov.dk/eid/Professional/DK",
            };
        }

        private static void ApplyNemLoginProfileDefaults(SamlUpPartyViewModel model)
        {
            if (IsOiosaml400SectorInternal(model.Modules.NemLogin.Sector))
            {
                ApplyNemLoginAttributeProfileDefaults(model);
                return;
            }

            ApplyNemLoginOiosaml303Defaults(model);
        }

        private static void ApplyNemLoginOiosaml303Defaults(SamlUpPartyViewModel model)
        {
            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles ??= new List<string>();
            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Clear();

            var requestedAttributeNames = GetNemLoginDefaultClaims();
            if (model.Modules?.NemLogin?.Sector == NemLoginSectors.PrivateOiosaml303)
            {
                requestedAttributeNames.Remove("https://data.gov.dk/model/core/eid/cprNumber");
            }
            model.Claims = requestedAttributeNames.ToList();
            EnsureNemLoginMetadataDefaults(model, requestedAttributeNames);
        }

        private static void UpdateNemLoginAuthnContextValue(SamlUpPartyViewModel model, string value, bool isEnabled)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return;
            }

            model.AuthnContextClassReferences ??= new List<string>();

            if (isEnabled)
            {
                if (!model.AuthnContextClassReferences.Any(v => v.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    model.AuthnContextClassReferences.Add(value);
                }
            }
            else
            {
                model.AuthnContextClassReferences.RemoveAll(v => v.Equals(value, StringComparison.OrdinalIgnoreCase));
            }

            EnsureNemLoginAuthnContextDefaults(model);
        }

        private static void ApplyNemLoginAttributeProfileDefaults(SamlUpPartyViewModel model)
        {
            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles ??= new List<string>();
            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = model.Modules.NemLogin.NemLoginRequestedAttributeProfiles
                .Where(p => !p.IsNullOrWhiteSpace())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Count == 0)
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = GetNemLoginDefaultAttributeProfileIds().ToList();
            }

            var requestedAttributeNames = GetNemLoginClaimsFromAttributeProfileIds(model.Modules.NemLogin.NemLoginRequestedAttributeProfiles);
            model.Claims = requestedAttributeNames.ToList();

            EnsureNemLoginMetadataDefaults(model, requestedAttributeNames);
        }

        private static IReadOnlyCollection<string> GetNemLoginClaimsFromAttributeProfileIds(IEnumerable<string> attributeProfileIds)
        {
            var requestedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "https://data.gov.dk/concept/core/nsis/loa",
                "https://data.gov.dk/model/core/specVersion"
            };

            foreach (var profileId in attributeProfileIds.Where(p => !p.IsNullOrWhiteSpace()))
            {
                if (profileId.StartsWith("https://data.gov.dk/eid/Person/DK", StringComparison.OrdinalIgnoreCase))
                {
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/email");
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/firstName");
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/lastName");

                    if (profileId.Contains("/WithoutCPR", StringComparison.OrdinalIgnoreCase) ||
                        profileId.Contains("/Anonymous", StringComparison.OrdinalIgnoreCase))
                    {
                        requestedAttributes.Add("https://data.gov.dk/model/core/eid/alias");
                    }
                    else
                    {
                        requestedAttributes.Add("https://data.gov.dk/model/core/eid/cprNumber");
                    }
                }
                else if (profileId.StartsWith("https://data.gov.dk/eid/Professional/DK", StringComparison.OrdinalIgnoreCase))
                {
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/email");
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/firstName");
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/lastName");

                    if (profileId.Contains("/Anonymous", StringComparison.OrdinalIgnoreCase))
                    {
                        requestedAttributes.Add("https://data.gov.dk/model/core/eid/alias");
                    }

                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/professional/cvr");
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/professional/orgName");
                    requestedAttributes.Add("https://data.gov.dk/model/core/eid/professional/uuid/persistent");
                }
            }

            return requestedAttributes;
        }

        private static void LoadNemLoginAuthnRequestExtensionsXml(SamlUpPartyViewModel model, bool loadRequestedAttributeProfiles)
        {
            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles ??= new List<string>();
            model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Clear();

            model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled = false;
            model.Modules.NemLogin.NemLoginAppSwitchAndroidReturnUrl = null;
            model.Modules.NemLogin.NemLoginAppSwitchIosEnabled = false;
            model.Modules.NemLogin.NemLoginAppSwitchIosReturnUrl = null;

            if (model.AuthnRequestExtensionsXml.IsNullOrWhiteSpace())
            {
                TryLoadNemLoginAppSwitchFromProfiles(model);
                return;
            }

            try
            {
                var wrapper = XElement.Parse($"<root>{model.AuthnRequestExtensionsXml}</root>");
                if (loadRequestedAttributeProfiles)
                {
                    foreach (var element in wrapper.Elements())
                    {
                        if (element.Name.NamespaceName == NemLoginExtensionsNamespace && element.Name.LocalName == "RequestedAttributeProfiles")
                        {
                            foreach (var profileElement in element.Elements())
                            {
                                if (profileElement.Name.NamespaceName != NemLoginExtensionsNamespace || profileElement.Name.LocalName != "Profile")
                                {
                                    continue;
                                }

                                var profileId = profileElement.Value?.Trim();
                                if (!profileId.IsNullOrWhiteSpace())
                                {
                                    model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Add(profileId);
                                }
                            }
                        }
                    }

                    model.Modules.NemLogin.NemLoginRequestedAttributeProfiles = model.Modules.NemLogin.NemLoginRequestedAttributeProfiles
                        .Where(p => !p.IsNullOrWhiteSpace())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
            catch
            {
                model.Modules.NemLogin.NemLoginRequestedAttributeProfiles.Clear();
            }

            if (TryLoadNemLoginAppSwitchFromProfiles(model))
            {
                return;
            }

            if (TryReadNemLoginAppSwitch(model.AuthnRequestExtensionsXml, out var platform, out var returnUrl))
            {
                ApplyNemLoginAppSwitchSetting(model, platform, returnUrl, profileName: null);
            }
        }
        private static void UpdateNemLoginAuthnRequestExtensionsXml(SamlUpPartyViewModel model)
        {
            var requestedAttributeProfiles = IsOiosaml400SectorInternal(model.Modules.NemLogin.Sector)
                ? model.Modules.NemLogin.NemLoginRequestedAttributeProfiles
                : null;

            model.AuthnRequestExtensionsXml = BuildNemLoginAuthnRequestExtensionsXml(
                model.AuthnRequestExtensionsXml,
                requestedAttributeProfiles,
                includeAppSwitch: false,
                appSwitchPlatform: null,
                appSwitchReturnUrl: null);

            UpdateNemLoginAppSwitchProfiles(model, requestedAttributeProfiles);
        }

        private static void UpdateNemLoginAppSwitchProfiles(SamlUpPartyViewModel model, IEnumerable<string> requestedAttributeProfiles)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            if (model.Profiles == null || model.Profiles.Count == 0)
            {
                if (!model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled && !model.Modules.NemLogin.NemLoginAppSwitchIosEnabled)
                {
                    return;
                }

                model.Profiles = new List<SamlUpPartyProfileViewModel>();
            }

            UpdateNemLoginAppSwitchProfile(
                model,
                NemLoginAppSwitchProfileDefinitions[0],
                model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled,
                model.Modules.NemLogin.NemLoginAppSwitchAndroidReturnUrl,
                requestedAttributeProfiles);

            UpdateNemLoginAppSwitchProfile(
                model,
                NemLoginAppSwitchProfileDefinitions[1],
                model.Modules.NemLogin.NemLoginAppSwitchIosEnabled,
                model.Modules.NemLogin.NemLoginAppSwitchIosReturnUrl,
                requestedAttributeProfiles);

            if (!model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled && !model.Modules.NemLogin.NemLoginAppSwitchIosEnabled)
            {
                model.Profiles.RemoveAll(profile => IsNemLoginAppSwitchProfileName(profile?.Name));
            }
        }

        private static void UpdateNemLoginAppSwitchProfile(SamlUpPartyViewModel model, NemLoginAppSwitchProfileDefinition definition, bool enabled, string returnUrl, IEnumerable<string> requestedAttributeProfiles)
        {
            if (!enabled)
            {
                model.Profiles.RemoveAll(profile => profile != null && string.Equals(profile.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
                return;
            }

            var profile = model.Profiles.FirstOrDefault(p => string.Equals(p.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
            {
                profile = new SamlUpPartyProfileViewModel
                {
                    Name = definition.Name,
                    DisplayName = definition.DisplayName
                };
                model.Profiles.Add(profile);
            }
            else if (profile.DisplayName.IsNullOrWhiteSpace())
            {
                profile.DisplayName = definition.DisplayName;
            }

            profile.AuthnRequestExtensionsXml = BuildNemLoginAuthnRequestExtensionsXml(
                profile.AuthnRequestExtensionsXml,
                requestedAttributeProfiles: requestedAttributeProfiles,
                includeAppSwitch: true,
                appSwitchPlatform: definition.Platform,
                appSwitchReturnUrl: returnUrl);
        }

        private static bool IsNemLoginAppSwitchProfileName(string profileName)
        {
            if (profileName.IsNullOrWhiteSpace())
            {
                return false;
            }

            return profileName.Equals(NemLoginAppSwitchAndroidProfileName, StringComparison.OrdinalIgnoreCase) || profileName.Equals(NemLoginAppSwitchIosProfileName, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildNemLoginAuthnRequestExtensionsXml(string existingXml, IEnumerable<string> requestedAttributeProfiles, bool includeAppSwitch, string appSwitchPlatform, string appSwitchReturnUrl)
        {
            var elements = new List<XElement>();

            if (!existingXml.IsNullOrWhiteSpace())
            {
                try
                {
                    var existingWrapper = XElement.Parse($"<root>{existingXml}</root>");
                    elements.AddRange(existingWrapper.Elements().Where(e =>
                        !(e.Name.NamespaceName == NemLoginExtensionsNamespace && e.Name.LocalName == "RequestedAttributeProfiles") &&
                        !(e.Name.NamespaceName == NemLoginExtensionsNamespace && e.Name.LocalName == "AppSwitch")));
                }
                catch
                {
                    elements.Clear();
                }
            }

            var hasProfiles = requestedAttributeProfiles?.Any(p => !p.IsNullOrWhiteSpace()) == true;
            if (hasProfiles)
            {
                XNamespace ns = NemLoginExtensionsNamespace;
                elements.Insert(0, new XElement(ns + "RequestedAttributeProfiles",
                    requestedAttributeProfiles
                        .Where(p => !p.IsNullOrWhiteSpace())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .Select(p => new XElement(ns + "Profile", p))));
            }

            if (includeAppSwitch)
            {
                XNamespace ns = NemLoginExtensionsNamespace;
                var platform = appSwitchPlatform.IsNullOrWhiteSpace() ? NemLoginAppSwitchPlatformAndroid : appSwitchPlatform.Trim();
                var appSwitchElement = new XElement(ns + "AppSwitch",
                    new XElement(ns + "Platform", platform));

                if (!appSwitchReturnUrl.IsNullOrWhiteSpace())
                {
                    appSwitchElement.Add(new XElement(ns + "ReturnURL", appSwitchReturnUrl.Trim()));
                }

                elements.Insert(hasProfiles ? 1 : 0, appSwitchElement);
            }

            return elements.Count == 0 ? null : string.Join(Environment.NewLine, elements.Select(e => e.ToString()));
        }

        private static bool TryLoadNemLoginAppSwitchFromProfiles(SamlUpPartyViewModel model)
        {
            if (model?.Profiles == null || model.Profiles.Count == 0)
            {
                return false;
            }

            var loaded = false;
            foreach (var definition in NemLoginAppSwitchProfileDefinitions)
            {
                var profile = model.Profiles.FirstOrDefault(p => string.Equals(p.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
                if (TryReadNemLoginAppSwitch(profile?.AuthnRequestExtensionsXml, out var platform, out var returnUrl))
                {
                    loaded |= ApplyNemLoginAppSwitchSetting(model, platform, returnUrl, profile?.Name ?? definition.Name);
                }
            }

            foreach (var profile in model.Profiles)
            {
                if (TryReadNemLoginAppSwitch(profile.AuthnRequestExtensionsXml, out var platform, out var returnUrl))
                {
                    loaded |= ApplyNemLoginAppSwitchSetting(model, platform, returnUrl, profile?.Name);
                }
            }

            return loaded;
        }
        private static bool ApplyNemLoginAppSwitchSetting(SamlUpPartyViewModel model, string platform, string returnUrl, string profileName)
        {
            if (model == null)
            {
                return false;
            }

            var normalizedPlatform = platform?.Trim();
            if (normalizedPlatform.IsNullOrWhiteSpace() && !profileName.IsNullOrWhiteSpace())
            {
                normalizedPlatform = profileName.Trim();
            }

            if (normalizedPlatform.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (normalizedPlatform.Equals(NemLoginAppSwitchPlatformAndroid, StringComparison.OrdinalIgnoreCase) || normalizedPlatform.Equals(NemLoginAppSwitchAndroidProfileName, StringComparison.OrdinalIgnoreCase))
            {
                model.Modules.NemLogin.NemLoginAppSwitchAndroidEnabled = true;
                if (!returnUrl.IsNullOrWhiteSpace())
                {
                    model.Modules.NemLogin.NemLoginAppSwitchAndroidReturnUrl = returnUrl;
                }
                return true;
            }

            if (normalizedPlatform.Equals(NemLoginAppSwitchPlatformIos, StringComparison.OrdinalIgnoreCase) || normalizedPlatform.Equals(NemLoginAppSwitchIosProfileName, StringComparison.OrdinalIgnoreCase))
            {
                model.Modules.NemLogin.NemLoginAppSwitchIosEnabled = true;
                if (!returnUrl.IsNullOrWhiteSpace())
                {
                    model.Modules.NemLogin.NemLoginAppSwitchIosReturnUrl = returnUrl;
                }
                return true;
            }

            return false;
        }

        private static bool TryReadNemLoginAppSwitch(string extensionsXml, out string platform, out string returnUrl)
        {
            platform = null;
            returnUrl = null;

            if (extensionsXml.IsNullOrWhiteSpace())
            {
                return false;
            }

            try
            {
                var wrapper = XElement.Parse($"<root>{extensionsXml}</root>");
                var appSwitchElement = wrapper.Elements()
                    .FirstOrDefault(e => e.Name.NamespaceName == NemLoginExtensionsNamespace && e.Name.LocalName == "AppSwitch");

                if (appSwitchElement == null)
                {
                    return false;
                }

                var platformElement = appSwitchElement.Elements()
                    .FirstOrDefault(e => e.Name.NamespaceName == NemLoginExtensionsNamespace && e.Name.LocalName == "Platform");
                platform = platformElement?.Value?.Trim();

                var returnUrlElement = appSwitchElement.Elements()
                    .FirstOrDefault(e => e.Name.NamespaceName == NemLoginExtensionsNamespace && e.Name.LocalName == "ReturnURL");
                returnUrl = returnUrlElement?.Value?.Trim();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureNemLoginMetadataDefaults(SamlUpPartyViewModel model, IReadOnlyCollection<string> requestedAttributeNames)
        {
            model.MetadataAttributeConsumingServices ??= new List<SamlMetadataAttributeConsumingService>();
            model.MetadataContactPersons ??= new List<SamlMetadataContactPerson>();

            requestedAttributeNames ??= Array.Empty<string>();

            if (model.MetadataAttributeConsumingServices.Count == 0)
            {
                model.MetadataAttributeConsumingServices.Add(new SamlMetadataAttributeConsumingService
                {
                    ServiceName = new SamlMetadataServiceName { Lang = "en", Name = "NemLog-in service" },
                    RequestedAttributes = CreateNemLoginRequestedAttributes(requestedAttributeNames)
                });
            }
            else
            {
                model.MetadataAttributeConsumingServices[0].ServiceName ??= new SamlMetadataServiceName { Lang = "en", Name = "NemLog-in service" };
                model.MetadataAttributeConsumingServices[0].RequestedAttributes = CreateNemLoginRequestedAttributes(requestedAttributeNames);
            }

            if (!model.MetadataContactPersons.Any(cp => cp.ContactType == SamlMetadataContactPersonTypes.Technical))
            {
                model.MetadataContactPersons.Add(new SamlMetadataContactPerson
                {
                    ContactType = SamlMetadataContactPersonTypes.Technical
                });
            }
        }

        private static List<SamlMetadataRequestedAttribute> CreateNemLoginRequestedAttributes(IEnumerable<string> requestedAttributeNames)
        {
            const string uriNameFormat = "urn:oasis:names:tc:SAML:2.0:attrname-format:uri";

            return requestedAttributeNames
                .Where(c => !c.IsNullOrWhiteSpace())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .Select(c => new SamlMetadataRequestedAttribute
                {
                    Name = c,
                    NameFormat = uriNameFormat,
                    IsRequired = c.Equals("https://data.gov.dk/concept/core/nsis/loa", StringComparison.OrdinalIgnoreCase)
                })
                .ToList();
        }

        private static void EnsureNemLoginAuthnContextDefaults(SamlUpPartyViewModel model)
        {
            model.AuthnContextClassReferences ??= new List<string>();

            var isOiosaml303 = IsOiosaml303SectorInternal(model.Modules.NemLogin.Sector);
            var loaValues = GetNemLoginLoaValues(isOiosaml303);

            var loa = GetNemLoginLoaValue(model.Modules.NemLogin.NemLoginMinimumLoa, loaValues, isOiosaml303)
                ?? GetNemLoginLoaValue(model.AuthnContextClassReferences.FirstOrDefault(IsNemLoginLoaValue), loaValues, isOiosaml303);

            if (isOiosaml303)
            {
                var extraValues = model.AuthnContextClassReferences
                    .Where(value => !IsNemLoginLoaValue(value) && NemLoginOiosaml303AuthnContextValues.Contains(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var authnContextValues = new List<string>();
                if (!loa.IsNullOrWhiteSpace())
                {
                    authnContextValues.Add(loa);
                }
                authnContextValues.AddRange(extraValues);
                model.AuthnContextClassReferences = authnContextValues;
            }
            else
            {
                model.AuthnContextClassReferences = loa.IsNullOrWhiteSpace()
                    ? new List<string>()
                    : new List<string> { loa };
            }
            model.Modules.NemLogin.NemLoginMinimumLoa = loa;

            if (model.AuthnContextClassReferences.Count == 0)
            {
                model.AuthnContextComparisonViewModel = SamlAuthnContextComparisonTypesVievModel.Null;
                return;
            }

            if (!Enum.IsDefined(typeof(SamlAuthnContextComparisonTypesVievModel), model.AuthnContextComparisonViewModel) ||
                model.AuthnContextComparisonViewModel == SamlAuthnContextComparisonTypesVievModel.Null)
            {
                model.AuthnContextComparisonViewModel = SamlAuthnContextComparisonTypesVievModel.Minimum;
            }
        }

        private static bool HasCompleteNemLoginContactPerson(IEnumerable<SamlMetadataContactPerson> contactPersons)
        {
            if (contactPersons == null)
            {
                return false;
            }

            foreach (var contactPerson in contactPersons)
            {
                if (contactPerson.Company.IsNullOrWhiteSpace() ||
                    contactPerson.EmailAddress.IsNullOrWhiteSpace())
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static HashSet<string> GetNemLoginLoaValues(bool isOiosaml303)
        {
            var prefix = isOiosaml303 ? NemLoginOiosaml303LoaPrefix : NemLoginOiosaml400LoaPrefix;
            return new HashSet<string>(NemLoginLoaLevels.Select(level => $"{prefix}{level}"), StringComparer.OrdinalIgnoreCase);
        }

        private static string GetNemLoginLoaValue(string value, HashSet<string> loaValues, bool isOiosaml303)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (loaValues.Contains(value))
            {
                return value;
            }

            return TryGetNemLoginLoaLevel(value, out var level) ? BuildNemLoginLoaValue(level, isOiosaml303) : null;
        }

        private static bool IsNemLoginLoaValue(string value)
        {
            return TryGetNemLoginLoaLevel(value, out _);
        }

        private static bool TryGetNemLoginLoaLevel(string value, out string level)
        {
            level = null;
            if (value.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (value.StartsWith(NemLoginOiosaml400LoaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                level = value.Substring(NemLoginOiosaml400LoaPrefix.Length);
            }
            else if (value.StartsWith(NemLoginOiosaml303LoaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                level = value.Substring(NemLoginOiosaml303LoaPrefix.Length);
            }

            if (level.IsNullOrWhiteSpace())
            {
                return false;
            }

            foreach (var allowedLevel in NemLoginLoaLevels)
            {
                if (allowedLevel.Equals(level, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildNemLoginLoaValue(string level, bool isOiosaml303)
        {
            var prefix = isOiosaml303 ? NemLoginOiosaml303LoaPrefix : NemLoginOiosaml400LoaPrefix;
            return $"{prefix}{level}";
        }
        private void ApplyNemLoginCprFlow(SamlUpPartyViewModel model, bool setExternalUserLifetime = false)
        {
            if (model?.ModuleType != UpPartyModuleTypes.NemLogin)
            {
                return;
            }

            EnsureNemLoginModule(model);

            var isCprEnabled = IsPrivateSectorInternal(model.Modules.NemLogin.Sector) && model.Modules.NemLogin.RequestCpr;
            if (!isCprEnabled)
            {
                model.ExtendedUis?.RemoveAll(e => e.ModuleType == ExtendedUiModuleTypes.NemLoginPrivateCprMatch);
                model.ClaimTransforms?.RemoveAll(IsNemLoginCprTransform);
                RemoveNemLoginCprExternalUserConfiguration(model);
                return;
            }

            var cprExtendedUiName = EnsureNemLoginCprExtendedUi(model);

            model.ClaimTransforms ??= new List<ClaimTransformViewModel>();
            model.ClaimTransforms.RemoveAll(IsNemLoginCprTransform);

            if (!model.Modules.NemLogin.SaveCprOnExternalUsers)
            {
                model.ClaimTransforms.Add(new SamlClaimTransformClaimInClaimOutViewModel
                {
                    Name = NemLoginCprTransformOpenExtendedUiName,
                    Type = ClaimTransformTypes.RegexMatch,
                    Action = ClaimTransformActions.Add,
                    ClaimIn = ClaimTypes.NameIdentifier,
                    ClaimOut = Constants.SamlClaimTypes.OpenExtendedUi,
                    Transformation = NemLoginCprSubjectUuidPattern,
                    TransformationExtension = cprExtendedUiName
                });

                RemoveNemLoginCprExternalUserConfiguration(model);
                return;
            }

            model.ClaimTransforms.Add(new SamlClaimTransformClaimInClaimsOutViewModel
            {
                Name = NemLoginCprTransformQueryExternalUserName,
                Type = ClaimTransformTypes.MatchClaim,
                Task = ClaimTransformTasks.QueryExternalUser,
                Action = ClaimTransformActions.Replace,
                ClaimIn = ClaimTypes.NameIdentifier,
                ClaimsOut = new List<string> { Constants.JwtClaimTypes.Modules.CprNumber },
                Transformation = Constants.ClaimTransformClaimTypes.ExternalUserLink,
                UpPartyName = model.Name
            });

            model.ClaimTransforms.Add(new SamlClaimTransformClaimsInClaimOutViewModel
            {
                Name = NemLoginCprTransformGuardName,
                Type = ClaimTransformTypes.Concatenate,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = new List<string>
                {
                    ClaimTypes.NameIdentifier,
                    Constants.JwtClaimTypes.Modules.CprNumber
                },
                ClaimOut = NemLoginCprGuardClaimType,
                Transformation = "{0}|{1}"
            });

            model.ClaimTransforms.Add(new SamlClaimTransformClaimInClaimOutViewModel
            {
                Name = NemLoginCprTransformRemoveJwtName,
                Type = ClaimTransformTypes.MatchClaim,
                Action = ClaimTransformActions.Remove,
                ClaimOut = Constants.JwtClaimTypes.Modules.CprNumber
            });

            model.ClaimTransforms.Add(new SamlClaimTransformClaimInClaimOutViewModel
            {
                Name = NemLoginCprTransformOpenUiNoMatchName,
                Type = ClaimTransformTypes.RegexMatch,
                Action = ClaimTransformActions.Add,
                ClaimIn = NemLoginCprGuardClaimType,
                ClaimOut = Constants.SamlClaimTypes.OpenExtendedUi,
                Transformation = NemLoginCprSubjectUuidWithEmptyCprPattern,
                TransformationExtension = cprExtendedUiName
            });

            model.LinkExternalUser ??= new LinkExternalUserViewModel();
            if (setExternalUserLifetime)
            {
                model.LinkExternalUser.ExternalUserLifetime = 0;
            }
            model.LinkExternalUser.AutoCreateUser = true;
            model.LinkExternalUser.RequireUser = false;
            model.LinkExternalUser.LinkClaimType = JwtClaimTypes.Subject;
            model.LinkExternalUser.RedemptionClaimType = null;
            model.LinkExternalUser.OverwriteClaims = true;
            model.LinkExternalUser.UpPartyClaims ??= new List<string>();
            model.LinkExternalUser.UpPartyClaims = model.LinkExternalUser.UpPartyClaims
                .Concat([Constants.JwtClaimTypes.Modules.CprNumber])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void RemoveNemLoginCprExternalUserConfiguration(SamlUpPartyViewModel model)
        {
            if (model?.LinkExternalUser == null)
            {
                return;
            }

            if (model.LinkExternalUser.UpPartyClaims?.Count > 0)
            {
                model.LinkExternalUser.UpPartyClaims = model.LinkExternalUser.UpPartyClaims
                    .Where(c => !c.Equals(Constants.JwtClaimTypes.Modules.CprNumber, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (model.LinkExternalUser.UpPartyClaims.Count == 0)
                {
                    model.LinkExternalUser.UpPartyClaims = null;
                }
            }

            if (model.LinkExternalUser.AutoCreateUser &&
                !model.LinkExternalUser.RequireUser &&
                model.LinkExternalUser.OverwriteClaims &&
                model.LinkExternalUser.LinkClaimType == JwtClaimTypes.Subject &&
                model.LinkExternalUser.RedemptionClaimType.IsNullOrWhiteSpace())
            {
                model.LinkExternalUser.AutoCreateUser = false;
                model.LinkExternalUser.OverwriteClaims = false;
                model.LinkExternalUser.LinkClaimType = null;
            }

            model.LinkExternalUser.ExternalUserLifetime = 0;
        }

        private static bool IsNemLoginCprTransform(ClaimTransformViewModel transform)
        {
            if (transform == null)
            {
                return false;
            }

            return !transform.Name.IsNullOrWhiteSpace() &&
                transform.Name.StartsWith(NemLoginCprTransformPrefix, StringComparison.Ordinal);
        }

        private static string EnsureNemLoginCprExtendedUi(SamlUpPartyViewModel model)
        {
            model.ExtendedUis ??= new List<ExtendedUiViewModel>();

            var extendedUi = model.ExtendedUis.FirstOrDefault(e => e.ModuleType == ExtendedUiModuleTypes.NemLoginPrivateCprMatch);
            if (extendedUi == null)
            {
                extendedUi = new ExtendedUiViewModel
                {
                    Name = NemLoginCprExtendedUiName,
                    ModuleType = ExtendedUiModuleTypes.NemLoginPrivateCprMatch,
                    Modules = new ExtendedUiModules
                    {
                        NemLogin = new ExtendedUiNemLoginModule
                        {
                            Environment = model.Modules.NemLogin.Environment
                        }
                    }
                };
                model.ExtendedUis.Add(extendedUi);
            }
            else
            {
                extendedUi.Name = NemLoginCprExtendedUiName;
                extendedUi.Modules ??= new ExtendedUiModules();
                extendedUi.Modules.NemLogin ??= new ExtendedUiNemLoginModule();
                extendedUi.Modules.NemLogin.Environment = model.Modules.NemLogin.Environment;
            }

            return extendedUi.Name;
        }

        public async Task<CertificateAndPassword> DownloadNemLoginTestCertificateAsync()
        {
            if (testCertificateTask == null)
            {
                testCertificateTask = DownloadNemLoginTestCertificateInternalAsync();
            }

            try
            {
                return await testCertificateTask;
            }
            catch
            {
                testCertificateTask = null;
                throw;
            }
        }

        public async Task<JwkWithCertificateInfo> GetNemLoginTestCertificateKeyAsync()
        {
            if (testCertificateKeyTask == null)
            {
                testCertificateKeyTask = GetNemLoginTestCertificateKeyInternalAsync();
            }

            try
            {
                return await testCertificateKeyTask;
            }
            catch
            {
                testCertificateKeyTask = null;
                throw;
            }
        }

        private async Task<CertificateAndPassword> DownloadNemLoginTestCertificateInternalAsync()
        {
            using var httpClient = httpClientFactory.CreateClient(BaseService.HttpClientLogicalName);

            var nemLoginAssets = clientSettings?.ModuleAssets?.NemLogin;
            if (nemLoginAssets == null)
            {
                throw new InvalidOperationException("NemLog-in asset URLs are not loaded.");
            }

            if (nemLoginAssets.TestCertificateUrl.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("NemLog-in test certificate URL is not configured.");
            }
            if (nemLoginAssets.TestCertificatePasswordUrl.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("NemLog-in test certificate password URL is not configured.");
            }

            var certificateBytes = await httpClient.GetByteArrayAsync(nemLoginAssets.TestCertificateUrl);
            var password = (await httpClient.GetStringAsync(nemLoginAssets.TestCertificatePasswordUrl)).Trim();

            return new CertificateAndPassword
            {
                EncodeCertificate = WebEncoders.Base64UrlEncode(certificateBytes),
                Password = password
            };
        }

        private async Task<JwkWithCertificateInfo> GetNemLoginTestCertificateKeyInternalAsync()
        {
            var certificateAndPassword = await DownloadNemLoginTestCertificateAsync();
            return await helpersService.ReadCertificateAsync(certificateAndPassword);
        }

        private async Task UploadTrackPrimaryCertificateAsync(CertificateAndPassword certificateAndPassword)
        {
            var jwkWithCertificateInfo = await helpersService.ReadCertificateAsync(certificateAndPassword);
            await UploadTrackPrimaryCertificateAsync(jwkWithCertificateInfo);
        }

        private async Task UploadTrackPrimaryCertificateAsync(JwkWithCertificateInfo jwkWithCertificateInfo)
        {
            await trackService.UpdateTrackKeyContainedAsync(new TrackKeyItemContainedRequest
            {
                IsPrimary = true,
                CreateSelfSigned = false,
                Key = jwkWithCertificateInfo
            });
        }
    }
}
