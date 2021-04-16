using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Logic
{
    public class SecurityHeaderLogic : LogicBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public SecurityHeaderLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
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
                return new List<string> { "*" };
            }
            return domains;
        }

        public void AddFormAction(string url = null)
        {
            var domains = HttpContext.Items.ContainsKey(Constants.SecurityHeader.FormActionDomains) ? HttpContext.Items[Constants.SecurityHeader.FormActionDomains] as List<string> : new List<string>();
            domains = AddUrlToDomains(domains, url);
            HttpContext.Items[Constants.SecurityHeader.FormActionDomains] = domains;
        }

        public void AddFormActionAllowAll()
        {
            HttpContext.Items[Constants.SecurityHeader.FormActionDomainsAllowAll] = true;
        }

        public List<string> GetFrameSrcDomains()
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameSrcDomains))
            {
                return AddAllowAllFrameSrcDomains(HttpContext.Items[Constants.SecurityHeader.FrameSrcDomains] as List<string>);
            }
            else
            {
                return AddAllowAllFrameSrcDomains(null);
            }
        }

        private List<string> AddAllowAllFrameSrcDomains(List<string> domains)
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameSrcDomainsAllowAll))
            {
                return new List<string> { "*" };
            }
            return domains;
        }

        public void AddFrameSrcUrls(IEnumerable<string> urls)
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

        public void AddFrameSrcAllowAll()
        {
            HttpContext.Items[Constants.SecurityHeader.FrameSrcDomainsAllowAll] = true;
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

        public void AddAllowIframeOnDomains(IEnumerable<string> domains)
        {
            var currentDomains = HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameAllowIframeOnDomains) ? HttpContext.Items[Constants.SecurityHeader.FrameAllowIframeOnDomains] as List<string> : new List<string>();
            foreach (var domain in domains)
            {
                currentDomains = currentDomains.ConcatOnce(domain);
            }
            HttpContext.Items[Constants.SecurityHeader.FrameAllowIframeOnDomains] = domains;
        }

        private List<string> AddUrlToDomains(List<string> domains, string url)
        {
            var domain = url.UrlToDomain();
            domains = domains.ConcatOnce(domain);
            return domains;
        }
    }
}
