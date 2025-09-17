using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Models.ViewModels;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class DynamicElementLogic : LogicBase
    {
        private readonly CountryCodesLogic countryCodesLogic;
        private readonly IStringLocalizer localizer;

        public DynamicElementLogic(CountryCodesLogic countryCodesLogic, IStringLocalizer localizer, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.countryCodesLogic = countryCodesLogic;
            this.localizer = localizer;
        }

        public IEnumerable<DynamicElementBase> ToElementsViewModel(List<DynamicElement> elements, List<DynamicElementBase> valueElements = null, List<Claim> initClaims = null)
        {
            if(elements == null)
            {
                yield break;
            }

            var countryCode = elements.Where(e => e.Type == DynamicElementTypes.Phone).Any() ? countryCodesLogic.GetCountryCodeStringByCulture() : null;

            var i = -1;
            foreach (var element in elements)
            {
                DynamicElementBase valueElement = null;
                if (element.Type != DynamicElementTypes.Text && element.Type != DynamicElementTypes.Html)
                {
                    i++;
                    valueElement = GetValueElement(element, valueElements, initClaims, i);
                }

                switch (element.Type)
                {
                    case DynamicElementTypes.Email:
                        yield return element.Required ? new EmailRequiredDElement { Name = element.Name, DField1 = valueElement?.DField1, IsUserIdentifier = element.IsUserIdentifier } : new EmailDElement { Name = element.Name, DField1 = valueElement?.DField1, IsUserIdentifier = element.IsUserIdentifier };
                        break;
                    case DynamicElementTypes.Phone:
                        var phoneDField1 = valueElement?.DField1;
                        if (phoneDField1.IsNullOrWhiteSpace())
                        {
                            phoneDField1 = countryCode;
                        }
                        yield return element.Required ? new PhoneRequiredDElement { Name = element.Name, DField1 = phoneDField1, IsUserIdentifier = element.IsUserIdentifier } : new PhoneDElement { Name = element.Name, DField1 = phoneDField1, IsUserIdentifier = element.IsUserIdentifier };
                        break;
                    case DynamicElementTypes.Username:
                        yield return element.Required ? new UsernameRequiredDElement { Name = element.Name, DField1 = valueElement?.DField1, IsUserIdentifier = element.IsUserIdentifier } : new UsernameDElement { Name = element.Name, DField1 = valueElement?.DField1, IsUserIdentifier = element.IsUserIdentifier };
                        break;
                    case DynamicElementTypes.EmailAndPassword:
                        yield return new EmailRequiredDElement { Name = element.Name, DField1 = valueElement?.DField1, IsUserIdentifier = true };
                        yield return new PasswordDElement { Name = element.Name, DField1 = valueElement?.DField1, DField2 = valueElement?.DField2 };
                        break;
                    case DynamicElementTypes.Password:
                        yield return new PasswordDElement { Name = element.Name, DField1 = valueElement?.DField1, DField2 = valueElement?.DField2 };
                        break;
                    case DynamicElementTypes.Name:
                        yield return element.Required ? new NameRequiredDElement { Name = element.Name, DField1 = valueElement?.DField1 } : new NameDElement { Name = element.Name, DField1 = valueElement?.DField1 };
                        break;
                    case DynamicElementTypes.GivenName:
                        yield return element.Required ? new GivenNameRequiredDElement { Name = element.Name, DField1 = valueElement?.DField1 } : new GivenNameDElement { Name = element.Name, DField1 = valueElement?.DField1 };
                        break;
                    case DynamicElementTypes.FamilyName:
                        yield return element.Required ? new FamilyNameRequiredDElement { Name = element.Name, DField1 = valueElement?.DField1 } : new FamilyNameDElement { Name = element.Name, DField1 = valueElement?.DField1 };
                        break;
                    case DynamicElementTypes.Custom:
                        yield return new CustomDElement { Name = element.Name, DField1 = valueElement?.DField1, Required = element.Required, DisplayName = element.DisplayName, MaxLength = element.MaxLength, RegEx = element.RegEx, ErrorMessage = element.ErrorMessage, ClaimOut = element.ClaimOut };
                        break;
                    case DynamicElementTypes.Text:
                        yield return new ContentDElement { Name = element.Name, DContent = element.Content };
                        break;
                    case DynamicElementTypes.Html:
                        yield return new ContentDElement { Name = element.Name, DContent = element.Content, IsHtml = true };
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public List<DynamicElement> EnsureLoginElements(List<DynamicElement> elements)
        {
            var list = elements ?? new List<DynamicElement>();

            var identifierElements = list.Where(e => e.Type == DynamicElementTypes.LoginInput).ToList();
            if (identifierElements.Count == 0)
            {
                var identifier = new DynamicElement
                {
                    Name = Constants.Models.DynamicElements.LoginInputElementName,
                    Type = DynamicElementTypes.LoginInput,
                    ShowOnIdentifier = true,
                    ShowOnPassword = false
                };
                list.Insert(0, identifier);
            }

            return list;
        }

        public List<DynamicElementBase> ToLoginDynamicElements(List<DynamicElement> elements, bool identifierPage)
        {
            var result = new List<DynamicElementBase>();
            if (elements?.Count > 0)
            {
                foreach (var element in elements.OrderBy(e => e.Order))
                {
                    if (identifierPage && element.Type == DynamicElementTypes.LoginInput)
                    {
                        result.Add(new LognInputDElement { Name = element.Name });
                    }
                    else if (!identifierPage && element.Type == DynamicElementTypes.Password)
                    {
                        result.Add(new LognInputDElement { Name = element.Name });
                    }
                    else if (element.Type == DynamicElementTypes.Text || element.Type == DynamicElementTypes.Html)
                    {
                        result.Add(new ContentDElement
                        {
                            Name = element.Name,
                            DContent = element.Content,
                            IsHtml = element.Type == DynamicElementTypes.Html,
                        });
                    }
                }
            }

            return result;
        }

        private DynamicElementBase GetValueElement(DynamicElement element, List<DynamicElementBase> valueElements, List<Claim> initClaims, int i)
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

            return valueElement;
        }

        public async Task<(UserIdentifier userIdentifier, string password, int passwordIndex)> ValidateCreateUserViewModelElementsAsync(ModelStateDictionary modelState, List<DynamicElementBase> elements)
        {
            var userIdentifier = new UserIdentifier();
            var password = string.Empty;
            var passwordIndex = 0;
            var index = 0;
            foreach (var element in elements)
            {
                await ValidateViewModelElementAsync(modelState, element, index);

                if (element is EmailDElement)
                {
                    if (element.IsUserIdentifier)
                    {
                        userIdentifier.Email = element.DField1;
                    }
                }
                else if (element is PhoneDElement)
                {
                    if (element.IsUserIdentifier)
                    {
                        userIdentifier.Phone = element.DField1;
                    }
                }
                else if (element is UsernameDElement)
                {
                    if (element.IsUserIdentifier)
                    {
                        userIdentifier.Username = element.DField1;
                    }
                }
                else if (element is PasswordDElement)
                {
                    passwordIndex = index;
                    password = element.DField1;
                    element.DField1 = null;
                    element.DField2 = null;
                }
                index++;
            }
            return (userIdentifier, password, passwordIndex);
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
            string phoneTempValue = null;
            if (element is PhoneDElement)
            {
                phoneTempValue = element.DField1;
                element.DField1 = element.DField2 = countryCodesLogic.ReturnFullPhoneOnly(element.DField1);
            }

            if (!(element is ContentDElement))
            {
                var elementValidation = await element.ValidateObjectResultsAsync();
                if (!elementValidation.isValid)
                {
                    foreach (var result in elementValidation.results)
                    {
                        modelState.AddModelError($"Elements[{index}].{result.MemberNames.First()}", localizer[result.ErrorMessage, result.MemberNames]);
                    }
                }

                if (element is CustomDElement customDElement)
                {
                    if (!element.DField1.IsNullOrWhiteSpace() && !customDElement.RegEx.IsNullOrWhiteSpace() && !customDElement.ErrorMessage.IsNullOrWhiteSpace())
                    {
                        if (!Regex.IsMatch(element.DField1, customDElement.RegEx))
                        {
                            modelState.AddModelError($"Elements[{index}].{nameof(element.DField1)}", localizer[customDElement.ErrorMessage]);
                        }
                    }
                }

                index++;
            }

            if (element is PhoneDElement)
            {
                element.DField1 = phoneTempValue;
            }
        }

        public void SetModelElementError(ModelStateDictionary modelState, List<DynamicElementBase> elements, string name, string errorMessage)
        {
            var index = 0;
            foreach (var element in elements)
            {
                if (!(element is ContentDElement))
                {
                    if (element.Name == name)
                    {
                        modelState.AddModelError($"Elements[{index}].{nameof(element.DField1)}", localizer[errorMessage]);
                    }

                    index++;
                }
            }
        }

        public (List<Claim>, List<string>) GetClaims(List<DynamicElementBase> elements)
        {
            var claims = new List<Claim>();
            var userIdentifierClaimTypes = new List<string>();
            var emailDElament = elements.Where(e => e is EmailDElement).FirstOrDefault() as EmailDElement;
            if (!string.IsNullOrWhiteSpace(emailDElament?.DField1))
            {
                claims.AddClaim(JwtClaimTypes.Email, emailDElament.DField1);
                if (emailDElament.IsUserIdentifier)
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.Email);
                }
            }
            var phoneDElament = elements.Where(e => e is PhoneDElement).FirstOrDefault() as PhoneDElement;
            if (!string.IsNullOrWhiteSpace(phoneDElament?.DField2)) // Full phone only (DField2)
            {
                claims.AddClaim(JwtClaimTypes.PhoneNumber, phoneDElament.DField2);
                if (phoneDElament.IsUserIdentifier)
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.PhoneNumber);
                }
            }
            var usernameDElament = elements.Where(e => e is UsernameDElement).FirstOrDefault() as UsernameDElement;
            if (!string.IsNullOrWhiteSpace(usernameDElament?.DField1))
            {
                claims.AddClaim(JwtClaimTypes.PreferredUsername, usernameDElament.DField1);
                if (usernameDElament.IsUserIdentifier)
                {
                    userIdentifierClaimTypes.Add(JwtClaimTypes.PreferredUsername);
                }
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
            var CustomDElament = elements.Where(e => e is CustomDElement).FirstOrDefault() as CustomDElement;
            if (!string.IsNullOrWhiteSpace(CustomDElament?.ClaimOut) && !string.IsNullOrWhiteSpace(CustomDElament?.DField1))
            {
                claims.AddClaim(CustomDElament.ClaimOut, CustomDElament.DField1);
            }
            return (claims, userIdentifierClaimTypes);
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
                    case DynamicElementTypes.Phone:
                        return claims.Where(c => c.Type == JwtClaimTypes.PhoneNumber).FirstOrDefault();
                    case DynamicElementTypes.Username:
                        return claims.Where(c => c.Type == JwtClaimTypes.PreferredUsername).FirstOrDefault();
                    case DynamicElementTypes.Name:
                        return claims.Where(c => c.Type == JwtClaimTypes.Name).FirstOrDefault();
                    case DynamicElementTypes.GivenName:
                        return claims.Where(c => c.Type == JwtClaimTypes.GivenName).FirstOrDefault();
                    case DynamicElementTypes.FamilyName:
                        return claims.Where(c => c.Type == JwtClaimTypes.FamilyName).FirstOrDefault();
                    case DynamicElementTypes.Custom:
                        return string.IsNullOrWhiteSpace(element.ClaimOut) ? null : claims.Where(c => c.Type == element.ClaimOut).FirstOrDefault();
                    case DynamicElementTypes.Text:
                    case DynamicElementTypes.Html:
                    case DynamicElementTypes.LoginInput:
                    case DynamicElementTypes.LoginButton:
                    case DynamicElementTypes.LoginLink:
                    case DynamicElementTypes.LoginEnd:
                        return null;
                    default:
                        throw new NotSupportedException();
                }
            }
            return null;
        }
    }
}