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

        public IEnumerable<DynamicElementBase> ToElementsViewModel(List<DynamicElement> elements, List<DynamicElementBase> valueElements = null, bool requireEmailAndPasswordElement = false, List<Claim> initClaims = null)
        {
            if(elements == null)
            {
                yield break;
            }

            bool hasEmailAndPasswordDElement = false;
            var i = 0;
            foreach (var element in elements)
            {
                var valueElement = valueElements?.Count() > i ? valueElements[i] : null;
                if (valueElement == null) 
                {
                    var valueClaim = FilterElementClaim(element, initClaims);
                    if (valueClaim != null)
                    {
                        valueElement = new DynamicElementBase { DField1 = valueClaim.Value };
                    }
                }
                switch (element.Type)
                {
                    case DynamicElementTypes.Email:
                        yield return element.Required ? new EmailRequiredDElement { DField1 = valueElement?.DField1 } : new EmailDElement { DField1 = valueElement?.DField1 };
                        break;
                    case DynamicElementTypes.EmailAndPassword:
                        hasEmailAndPasswordDElement = true;
                        yield return new EmailAndPasswordDElement { DField1 = valueElement?.DField1, DField2 = valueElement?.DField2, DField3 = valueElement?.DField3 };
                        break;
                    case DynamicElementTypes.Name:
                        yield return element.Required ? new NameRequiredDElement { DField1 = valueElement?.DField1 } : new NameDElement { DField1 = valueElement?.DField1 };
                        break;
                    case DynamicElementTypes.GivenName:
                        yield return element.Required ? new GivenNameRequiredDElement { DField1 = valueElement?.DField1 } : new GivenNameDElement { DField1 = valueElement?.DField1 };
                        break;
                    case DynamicElementTypes.FamilyName:
                        yield return element.Required ? new FamilyNameRequiredDElement { DField1 = valueElement?.DField1 } : new FamilyNameDElement { DField1 = valueElement?.DField1 };
                        break;
                    default:
                        throw new NotImplementedException();
                }
                i++;
            }
            if (requireEmailAndPasswordElement && !hasEmailAndPasswordDElement)
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
            var emailDElament = elements.Where(e => e is EmailDElement).FirstOrDefault() as EmailDElement;
            if (!string.IsNullOrWhiteSpace(emailDElament?.DField1))
            {
                claims.AddClaim(JwtClaimTypes.Email, emailDElament.DField1);
            }
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

        public Claim FilterElementClaim(DynamicElement element, IEnumerable<Claim> claims)
        {
            if (claims?.Count() > 0)
            {
                switch (element.Type)
                {
                    case DynamicElementTypes.Email:
                    case DynamicElementTypes.EmailAndPassword:
                        return claims.Where(c => c.Type == JwtClaimTypes.Email).FirstOrDefault();
                    case DynamicElementTypes.Name:
                        return claims.Where(c => c.Type == JwtClaimTypes.Name).FirstOrDefault();
                    case DynamicElementTypes.GivenName:
                        return claims.Where(c => c.Type == JwtClaimTypes.GivenName).FirstOrDefault();
                    case DynamicElementTypes.FamilyName:
                        return claims.Where(c => c.Type == JwtClaimTypes.FamilyName).FirstOrDefault();
                    default:
                        throw new NotSupportedException();
                }
            }
            return null;
        }
    }
}
