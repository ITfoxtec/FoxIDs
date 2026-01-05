using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels;

public class SamlUpPartyModulesViewModel : SamlUpPartyModules
{
    [ValidateComplexType]
    public new SamlUpPartyNemLoginModuleViewModel NemLogin { get; set; }
}
