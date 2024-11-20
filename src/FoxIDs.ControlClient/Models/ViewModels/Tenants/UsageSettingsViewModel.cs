using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UsageSettingsViewModel : UsageSettings
    {
        public UsageSettingsViewModel()
        {
            CurrencyExchanges = new List<UsageCurrencyExchange>();
        }
    }
}
