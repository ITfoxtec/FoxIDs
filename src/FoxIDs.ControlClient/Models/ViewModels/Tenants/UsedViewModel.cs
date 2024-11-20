using FoxIDs.Models.Api;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public class UsedViewModel : Used
    {
        public UsedViewModel()
        {
            Items = new List<UsedItem>();
        }
    }
}
