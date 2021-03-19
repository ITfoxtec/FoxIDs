using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs
{
    public static class Saml2BindingExtensions
    {
        public static Task<ContentResult> ToActionFormResultAsync(this Saml2RedirectBinding binding)
        {
            var urlSplit = binding.RedirectLocation.OriginalString.Split('?');
            if(urlSplit?.Count() != 2)
            {
                throw new InvalidSaml2BindingException($"Invalid Saml2RedirectBinding URL '{binding.RedirectLocation.OriginalString}'.");
            }
            var nameValueCollection = QueryHelpers.ParseQuery(urlSplit[1]).ToDictionary();

            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = nameValueCollection.ToHtmlGetPage(urlSplit[0]),
            });
        }

        public static Task<ContentResult> ToActionFormResultAsync(this Saml2PostBinding binding)
        {
            return Task.FromResult(new ContentResult
            {
                ContentType = "text/html",
                Content = binding.PostContent,
            });
        }
    }
}
