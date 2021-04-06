using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Logic
{
    public class SecurityHeaderLogic : LogicBase
    {
        public SecurityHeaderLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public void AddFormActionAllowAll()
        {
            HttpContext.Items[Constants.SecurityHeader.FormActionDomainsAllowAll] = true;
        }

        public List<string> GetFormActionDomains()
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FormActionDomains))
            {
                return AddAllowAllFormActionDomains(HttpContext.Items[Constants.SecurityHeader.FormActionDomains] as List<string>);
            }
            else
            {
                return AddAllowAllFormActionDomains(null);
            }
        }

        private List<string> AddAllowAllFormActionDomains(List<string> domains)
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FormActionDomainsAllowAll))
            {
                domains = domains ?? new List<string>();
                domains.Add("*");
            }
            return domains;
        }

        public void AddFormAction(string url = null)
        {
            var domains = HttpContext.Items.ContainsKey(Constants.SecurityHeader.FormActionDomains) ? HttpContext.Items[Constants.SecurityHeader.FormActionDomains] as List<string> : new List<string>();
            domains = AddUrlToDomains(domains, url);
            HttpContext.Items[Constants.SecurityHeader.FormActionDomains] = domains;
        }

        public List<string> GetFrameSrcDomains()
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameSrcDomains))
            {
                return HttpContext.Items[Constants.SecurityHeader.FrameSrcDomains] as List<string>;
            }
            else
            {
                return null;
            }
        }

        public void AddFrameSrc(IEnumerable<string> urls)
        {
            if (urls?.Count() > 0)
            {
                var domains = HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameSrcDomains) ? HttpContext.Items[Constants.SecurityHeader.FrameSrcDomains] as List<string> : new List<string>();
                foreach (var url in urls)
                {
                    domains = AddUrlToDomains(domains, url);
                }
                HttpContext.Items[Constants.SecurityHeader.FrameSrcDomains] = domains;
            }
        }

        public List<string> GetAllowIframeOnDomains()
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameAllowIframeOnDomains))
            {
                return HttpContext.Items[Constants.SecurityHeader.FrameAllowIframeOnDomains] as List<string>;
            }
            else
            {
                return null;
            }
        }

        public void AddAllowIframeOnUrls(IEnumerable<string> urls)
        {
            var domains = HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameAllowIframeOnDomains) ? HttpContext.Items[Constants.SecurityHeader.FrameAllowIframeOnDomains] as List<string> : new List<string>();
            foreach (var url in urls)
            {
                domains = AddUrlToDomains(domains, url);
            }
            HttpContext.Items[Constants.SecurityHeader.FrameAllowIframeOnDomains] = domains;
        }

        private List<string> AddUrlToDomains(List<string> domains, string url)
        {
            var domain = url.UrlToDomain();
            if (!string.IsNullOrEmpty(domain))
            {
                domains = domains.ConcatOnce(domain);
            }
            return domains;
        }
    }
}
