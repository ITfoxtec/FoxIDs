using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.ViewModels;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class DynamicElementLogic : LogicBase
    {
        public DynamicElementLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        public IEnumerable<DynamicElementBase> ToElementsViewModel(List<DynamicElement> elements, List<DynamicElementBase> valueElements = null, bool requireEmailAndPasswordElement = false)
        {
            if(elements == null)
            {
                yield break;
            }

            bool hasEmailAndPasswordElement = false;
            var i = 0;
            foreach (var element in elements)
            {
                var valueElement = valueElements?.Count() > i ? valueElements[i] : null;
                switch (element.Type)
                {
                    case DynamicElementTypes.EmailAndPassword:
                        hasEmailAndPasswordElement = true;
                        yield return new EmailAndPasswordDElement { DField1 = valueElement?.DField1, DField2 = valueElement?.DField2, DField3 = valueElement?.DField3, Required = true };
                        break;
                    case DynamicElementTypes.Name:
                        yield return new NameDElement { DField1 = valueElement?.DField1, Required = element.Required };
                        break;
                    case DynamicElementTypes.GivenName:
                        yield return new GivenNameDElement { DField1 = valueElement?.DField1, Required = element.Required };
                        break;
                    case DynamicElementTypes.FamilyName:
                        yield return new FamilyNameDElement { DField1 = valueElement?.DField1, Required = element.Required };
                        break;
                    default:
                        throw new NotImplementedException();
                }
                i++;
            }
            if (requireEmailAndPasswordElement && !hasEmailAndPasswordElement)
            {
                throw new Exception("The EmailAndPasswordDElement is required.");
            }
        }

        public async Task ValidateViewModelElementsAsync(ModelStateDictionary modelState, List<DynamicElementBase> elements)
        {
            var index = 0;
            foreach (var element in elements)
            {
                await ValidateViewModelElementAsync(modelState, element, index);
            }
        }

        public async Task ValidateViewModelElementAsync(ModelStateDictionary modelState, DynamicElementBase element, int index)
        {
            var elementValidation = await element.ValidateObjectResultsAsync();
            if (!elementValidation.isValid)
            {
                foreach (var result in elementValidation.results)
                {
                    modelState.AddModelError($"Elements[{index}].{result.MemberNames.First()}", result.ErrorMessage);
                }
            }
        }

        public List<Claim> GetClaims(List<DynamicElementBase> elements)
        {
            var claims = new List<Claim>();
            var nameDElament = elements.Where(e => e is NameDElement).FirstOrDefault() as NameDElement;
            if (!string.IsNullOrWhiteSpace(nameDElament?.DField1))
            {
                claims.AddClaim(JwtClaimTypes.Name, nameDElament.DField1);
            }
            var givenNameDElament = elements.Where(e => e is GivenNameDElement).FirstOrDefault() as GivenNameDElement;
            if (!string.IsNullOrWhiteSpace(givenNameDElament?.DField1))
            {
                claims.AddClaim(JwtClaimTypes.GivenName, givenNameDElament.DField1);
            }
            var familyNameDElament = elements.Where(e => e is FamilyNameDElement).FirstOrDefault() as FamilyNameDElement;
            if (!string.IsNullOrWhiteSpace(familyNameDElament?.DField1))
            {
                claims.AddClaim(JwtClaimTypes.FamilyName, familyNameDElament.DField1);
            }
            return claims;
        }
    }
}
