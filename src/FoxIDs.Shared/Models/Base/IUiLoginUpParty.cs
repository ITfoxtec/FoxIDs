using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IUiLoginUpParty : IDataDocument
    {
        bool DisableSetPasswordSms { get; set; }
        bool DisableSetPasswordEmail { get; set; }

        string Title { get; set; }
        string IconUrl { get; set; }
        string Css { get; set; }

        List<DynamicElement> Elements { get; set; }
    }
}