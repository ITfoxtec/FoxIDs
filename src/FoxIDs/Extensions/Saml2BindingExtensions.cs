using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using Microsoft.AspNetCore.Mvc;
using System;

namespace FoxIDs
{
    public static class Saml2BindingExtensions
    {
        public static IActionResult ToSamlActionResult(this Saml2Binding binding)
        {
            if (binding is Saml2RedirectBinding saml2RedirectBinding)
            {
                return saml2RedirectBinding.ToActionResult();
            }
            else if (binding is Saml2PostBinding saml2PostBinding)
            {
                return saml2PostBinding.ToActionResult();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
