using FoxIDs.Client.Logic;
using FoxIDs.Client.Logic.Modules;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages.Components.Modules;

public partial class SamlUpPartyMicrosoftEntraIdTemplate : ComponentBase
{
    private bool showSwitchToStandardConfirm;
    private bool showSwitchToTemplateConfirm;

    [Inject]
    public MicrosoftEntraIdUpPartyLogic MicrosoftEntraIdUpPartyLogic { get; set; }

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

        if (Model?.ModuleType != UpPartyModuleTypes.MicrosoftEntraId)
        {
            return;
        }

        MicrosoftEntraIdUpPartyLogic.EnsureMicrosoftEntraIdModule(Model);
        MicrosoftEntraIdUpPartyLogic.ApplyMicrosoftEntraIdTemplateDefaults(Model);
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
            Model.MetadataSingleLogout = null;
            GeneralSamlUpParty.ShowAuthorityDetails = false;
            return;
        }

        var (metadata, entityId, acs, singleLogout) = MetadataLogic.GetUpSamlMetadata(Model.Name, Model.PartyBindingPattern);
        Model.Metadata = metadata;
        Model.MetadataEntityId = string.IsNullOrWhiteSpace(Model.SpIssuer) ? entityId : Model.SpIssuer;
        Model.MetadataAcs = acs;
        Model.MetadataSingleLogout = singleLogout;
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
        if (Model == null)
        {
            return;
        }

        showSwitchToStandardConfirm = false;
        MicrosoftEntraIdUpPartyLogic.EnsureMicrosoftEntraIdModule(Model);
        Model.Modules.ShowStandardSettings = true;
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
        if (Model == null)
        {
            return;
        }

        showSwitchToTemplateConfirm = false;

        MicrosoftEntraIdUpPartyLogic.EnsureMicrosoftEntraIdModule(Model);
        MicrosoftEntraIdUpPartyLogic.ApplyMicrosoftEntraIdTemplateDefaults(Model);
        GeneralSamlUpParty.ShowAdvanced = false;
        Model.Modules.ShowStandardSettings = false;
        await ShowStandardSettingsChanged.InvokeAsync(false);
    }
}
