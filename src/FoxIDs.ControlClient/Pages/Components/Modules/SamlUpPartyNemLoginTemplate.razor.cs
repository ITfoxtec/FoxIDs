using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Logic;
using FoxIDs.Client.Logic.Modules;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Components.Modules;

public partial class SamlUpPartyNemLoginTemplate : ComponentBase
{
    private bool nemLoginDefaultCertificateLoaded;
    private bool showSwitchToStandardConfirm;
    private bool showSwitchToTemplateConfirm;

    [Inject]
    public NemLoginUpPartyLogic NemLoginUpPartyLogic { get; set; }

    [Inject]
    public HelpersService HelpersService { get; set; }

    [Inject]
    public OpenidConnectPkce OpenidConnectPkce { get; set; }

    [Inject]
    public MetadataLogic MetadataLogic { get; set; }

    [Parameter]
    public GeneralSamlUpPartyViewModel GeneralSamlUpParty { get; set; }

    private SamlUpPartyViewModel Model => GeneralSamlUpParty?.Form?.Model;

    [Parameter]
    public bool ShowStandardSettings { get; set; }

    [Parameter]
    public EventCallback<bool> ShowStandardSettingsChanged { get; set; }

    protected override void OnParametersSet()
    {
        if (ShowStandardSettings)
        {
            return;
        }

        if (Model?.ModuleType != UpPartyModuleTypes.NemLogin)
        {
            return;
        }

        NemLoginUpPartyLogic.EnsureNemLoginModule(Model);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (ShowStandardSettings)
        {
            return;
        }

        if (Model?.ModuleType != UpPartyModuleTypes.NemLogin)
        {
            return;
        }

        var environment = Model.Modules?.NemLogin?.Environment;
        if (environment != NemLoginEnvironments.IntegrationTest)
        {
            nemLoginDefaultCertificateLoaded = false;
            return;
        }

        if (!Model.Modules.NemLogin.NemLoginTrackCertificateBase64Url.IsNullOrWhiteSpace() ||
            Model.Modules.NemLogin.NemLoginTrackCertificateInfo != null ||
            nemLoginDefaultCertificateLoaded)
        {
            return;
        }

        nemLoginDefaultCertificateLoaded = true;
        await ReadNemLoginTrackCertificateAsync(useDefaultTestCertificate: true);
        await InvokeAsync(StateHasChanged);
    }

    private void EnsureSamlUpPartySummaryDefaults()
    {
        if (ShowStandardSettings)
        {
            return;
        }

        if (Model == null)
        {
            return;
        }

        if (Model.DisableUserAuthenticationTrust || Model.Name.IsNullOrWhiteSpace())
        {
            Model.Metadata = null;
            Model.MetadataEntityId = null;
            Model.MetadataAcs = null;
            return;
        }

        var (metadata, entityId, acs) = MetadataLogic.GetUpSamlMetadata(Model.Name, Model.PartyBindingPattern);
        Model.Metadata = metadata;
        Model.MetadataEntityId = string.IsNullOrWhiteSpace(Model.SpIssuer) ? entityId : Model.SpIssuer;
        Model.MetadataAcs = acs;
    }

    private async Task OnNemLoginTrackCertificateFileSelectedAsync(InputFileChangeEventArgs e)
    {
        if (e.File.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
        {
            GeneralSamlUpParty.Form.SetError($"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
            return;
        }

        Model.Modules.NemLogin.NemLoginTrackCertificateFileStatus = "Loading...";
        Model.Modules.NemLogin.NemLoginTrackCertificateError = null;
        Model.Modules.NemLogin.NemLoginTrackCertificateInfo = null;

        byte[] certificateBytes;
        using (var memoryStream = new MemoryStream())
        {
            using var fileStream = e.File.OpenReadStream(GeneralSamlUpPartyViewModel.CertificateMaxFileSize);
            await fileStream.CopyToAsync(memoryStream);
            certificateBytes = memoryStream.ToArray();
        }

        Model.Modules.NemLogin.NemLoginTrackCertificateBase64Url = WebEncoders.Base64UrlEncode(certificateBytes);
        Model.Modules.NemLogin.NemLoginTrackCertificateFileStatus = e.File.Name;
    }

    private async Task ReadNemLoginTrackCertificateAsync(bool useDefaultTestCertificate)
    {
        Model.Modules.NemLogin.NemLoginTrackCertificateError = null;
        Model.Modules.NemLogin.NemLoginTrackCertificateInfo = null;

        try
        {
            JwkWithCertificateInfo jwkWithCertificateInfo;
            var hasCustomCertificate = !Model.Modules.NemLogin.NemLoginTrackCertificateBase64Url.IsNullOrWhiteSpace();
            if (hasCustomCertificate)
            {
                var certificate = new CertificateAndPassword
                {
                    EncodeCertificate = Model.Modules.NemLogin.NemLoginTrackCertificateBase64Url,
                    Password = Model.Modules.NemLogin.NemLoginTrackCertificatePassword
                };

                jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(certificate);
            }
            else if (useDefaultTestCertificate && Model.Modules.NemLogin.Environment != NemLoginEnvironments.Production)
            {
                jwkWithCertificateInfo = await NemLoginUpPartyLogic.GetNemLoginTestCertificateKeyAsync();
            }
            else
            {
                Model.Modules.NemLogin.NemLoginTrackCertificateError = "Select a certificate first.";
                return;
            }

            if (!jwkWithCertificateInfo.HasPrivateKey())
            {
                Model.Modules.NemLogin.NemLoginTrackCertificateError = "Private key is required. Maybe a password is required to unlock the private key.";
                return;
            }

            Model.Modules.NemLogin.NemLoginTrackCertificateInfo = new KeyInfoViewModel
            {
                Subject = jwkWithCertificateInfo.CertificateInfo.Subject,
                ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom,
                ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo,
                IsValid = jwkWithCertificateInfo.CertificateInfo.IsValid(),
                Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint,
                Name = hasCustomCertificate ? null : "Default test certificate"
            };
        }
        catch (TokenUnavailableException)
        {
            if (OpenidConnectPkce is TenantOpenidConnectPkce tenantOpenidConnectPkce)
            {
                await tenantOpenidConnectPkce.TenantLoginAsync();
            }
        }
        catch (HttpRequestException ex)
        {
            Model.Modules.NemLogin.NemLoginTrackCertificateError = ex.Message;
        }
        catch (FoxIDsApiException ex)
        {
            Model.Modules.NemLogin.NemLoginTrackCertificateError = ex.Message;
        }
        catch (Exception ex)
        {
            Model.Modules.NemLogin.NemLoginTrackCertificateError = ex.Message;
        }
    }

    private async Task UseDefaultNemLoginTestCertificateAsync()
    {
        Model.Modules.NemLogin.NemLoginTrackCertificateBase64Url = null;
        Model.Modules.NemLogin.NemLoginTrackCertificatePassword = null;
        Model.Modules.NemLogin.NemLoginTrackCertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
        Model.Modules.NemLogin.NemLoginTrackCertificateInputFileKey = Guid.NewGuid();
        Model.Modules.NemLogin.NemLoginTrackCertificateEdit = false;

        await ReadNemLoginTrackCertificateAsync(useDefaultTestCertificate: true);
    }

    private void EnableNemLoginTrackCertificateEdit()
    {
        Model.Modules.NemLogin.NemLoginTrackCertificateEdit = true;
    }

    private void NemLoginEnvironmentChanged(NemLoginEnvironments environment)
    {
        NemLoginUpPartyLogic.HandleEnvironmentChanged(Model, environment);
        Model.Modules.NemLogin.NemLoginTrackCertificateBase64Url = null;
        Model.Modules.NemLogin.NemLoginTrackCertificatePassword = null;
        Model.Modules.NemLogin.NemLoginTrackCertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
        Model.Modules.NemLogin.NemLoginTrackCertificateInputFileKey = Guid.NewGuid();
        Model.Modules.NemLogin.NemLoginTrackCertificateInfo = null;
        Model.Modules.NemLogin.NemLoginTrackCertificateError = null;
        Model.Modules.NemLogin.NemLoginTrackCertificateEdit = false;
        nemLoginDefaultCertificateLoaded = false;
    }

    private void NemLoginMinimumLoaChanged(string loa)
    {
        NemLoginUpPartyLogic.HandleMinimumLoaChanged(Model, loa);
    }

    private void NemLoginOiosaml303IdTypeChanged(string idType, ChangeEventArgs e)
    {
        var isEnabled = e?.Value is bool b && b;
        NemLoginUpPartyLogic.HandleOiosaml303IdTypeChanged(Model, idType, isEnabled);
    }

    private void NemLoginOiosaml303CredentialTypeChanged(string credentialType, ChangeEventArgs e)
    {
        var isEnabled = e?.Value is bool b && b;
        NemLoginUpPartyLogic.HandleOiosaml303CredentialTypeChanged(Model, credentialType, isEnabled);
    }

    private void NemLoginRequestedAttributeProfileChanged(string profileId, ChangeEventArgs e)
    {
        var isEnabled = e?.Value is bool b && b;
        NemLoginUpPartyLogic.HandleRequestedAttributeProfileChanged(Model, profileId, isEnabled);
    }

    private void NemLoginAuthnRequestExtensionsChanged()
    {
        NemLoginUpPartyLogic.HandleAuthnRequestExtensionsChanged(Model);
    }

    private void NemLoginSectorChanged(NemLoginSectors sector)
    {
        NemLoginUpPartyLogic.HandleSectorChanged(Model, sector);
    }

    private void NemLoginCprFlowChanged()
    {
        NemLoginUpPartyLogic.HandleCprFlowChanged(Model);
    }

    private void RequestSwitchToStandard()
    {
        showSwitchToStandardConfirm = true;
        showSwitchToTemplateConfirm = false;
    }

    private void CancelSwitchToStandard()
    {
        showSwitchToStandardConfirm = false;
    }

    private async Task ConfirmSwitchToStandardAsync()
    {
        showSwitchToStandardConfirm = false;
        await ShowStandardSettingsChanged.InvokeAsync(true);
    }

    private void RequestSwitchToTemplate()
    {
        showSwitchToTemplateConfirm = true;
        showSwitchToStandardConfirm = false;
    }

    private void CancelSwitchToTemplate()
    {
        showSwitchToTemplateConfirm = false;
    }

    private async Task ConfirmSwitchToTemplateAsync()
    {
        showSwitchToTemplateConfirm = false;

        NemLoginUpPartyLogic.EnsureNemLoginModule(Model);
        NemLoginUpPartyLogic.ApplyNemLoginCreateDefaults(Model);
        Model.Modules.NemLogin.NemLoginTrackCertificateEdit = false;
        await ShowStandardSettingsChanged.InvokeAsync(false);
    }
}
