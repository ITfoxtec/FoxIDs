using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IUiLoginUpParty : IDataDocument
    {
        bool DisableSetPassword { get; set; }

        public string Title { get; set; }
        public string IconUrl { get; set; }
        string Css { get; set; }

        public List<DynamicElement> Elements { get; set; }
    }
}
