using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Logic.Modules;

public class MicrosoftEntraIdUpPartyLogic
{
    public void EnsureMicrosoftEntraIdModule(SamlUpPartyViewModel model)
    {
        if (model == null)
        {
            return;
        }

        model.Modules ??= new SamlUpPartyModulesViewModel();
    }

    public void ApplyMicrosoftEntraIdTemplateDefaults(SamlUpPartyViewModel model)
    {
        if (model?.ModuleType != UpPartyModuleTypes.MicrosoftEntraId)
        {
            return;
        }

        EnsureMicrosoftEntraIdModule(model);
        model.IsManual = false;
        model.PartyBindingPattern = PartyBindingPatterns.Tildes;
    }
}
