using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Client
{
    public static class DynamicElementExtensions
    {
        public static List<T> MapDynamicElementsAfterMap<T>(this List<T> elements) where T : DynamicElement
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

        public static List<T> EnsureLoginDynamicDefaults<T>(this List<T> elements) where T : DynamicElement, new()
        {
            elements ??= new List<T>();

            if (elements.Any(e => e.Type == DynamicElementTypes.LoginInput))
            {
                return elements;
            }

            var order = Constants.Models.DynamicElements.ElementsOrderMin;
            return new List<T>
            {
                new T
                {
                    Name = Constants.Models.DynamicElements.LoginInputElementName,
                    Type = DynamicElementTypes.LoginInput,
                    Order = order++,
                },
                new T
                {
                    Name = Constants.Models.DynamicElements.LoginButtonElementName,
                    Type = DynamicElementTypes.LoginButton,
                    Order = order++,
                },
                new T
                {
                    Name = Constants.Models.DynamicElements.LoginLinkElementName,
                    Type = DynamicElementTypes.LoginLink,
                    Order = order++,
                },
                new T
                {
                    Name = Constants.Models.DynamicElements.LoginHrdElementName,
                    Type = DynamicElementTypes.LoginHrd,
                    Order = order,
                }
            };
        }
    }
}