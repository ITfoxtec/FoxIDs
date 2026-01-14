using Microsoft.Extensions.DependencyInjection;
using System;

namespace FoxIDs.Client.Logic.Modules;

public class SamlUpPartyModuleLogic
{
    private readonly IServiceProvider serviceProvider;
    private Lazy<NemLoginUpPartyLogic> nemLoginUpPartyLogic;
    private Lazy<MicrosoftEntraIdUpPartyLogic> microsoftEntraIdUpPartyLogic;

    public SamlUpPartyModuleLogic(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public NemLoginUpPartyLogic NemLogin => (nemLoginUpPartyLogic ??= new Lazy<NemLoginUpPartyLogic>(
        () => serviceProvider.GetRequiredService<NemLoginUpPartyLogic>())).Value;

    public MicrosoftEntraIdUpPartyLogic MicrosoftEntraId => (microsoftEntraIdUpPartyLogic ??= new Lazy<MicrosoftEntraIdUpPartyLogic>(
        () => serviceProvider.GetRequiredService<MicrosoftEntraIdUpPartyLogic>())).Value;
}
