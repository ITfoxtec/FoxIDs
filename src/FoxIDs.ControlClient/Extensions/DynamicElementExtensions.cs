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

            var loginInputElements = elements.Where(e => e.Type == DynamicElementTypes.LoginInput).ToList();
            if (loginInputElements.Count == 0)
            {
                var identifier = new T
                {
                    Name = Constants.Models.DynamicElements.LoginInputElementName,
                    Type = DynamicElementTypes.LoginInput,
                    ShowOnIdentifier = true,
                    ShowOnPassword = false
                };
                elements.Insert(0, identifier);
            }

            return elements;
        }
    }
}
