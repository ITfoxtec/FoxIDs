using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Client
{
    public static class DynamicElementExtensions
    {
        public static List<DynamicElement> MapLinkExternalUserAfterMap(this List<DynamicElement> elements)
        {
            if (elements?.Count() > 0)
            {
                int order = 1;
                foreach (var element in elements)
                {
                    element.Order = order++;
                }
            }

            return elements;
        }
    }
}
