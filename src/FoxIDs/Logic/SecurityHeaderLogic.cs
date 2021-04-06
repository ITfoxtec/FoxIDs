using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Logic
{
    public class SecurityHeaderLogic : LogicBase
    {
        private readonly SequenceLogic sequenceLogic;

        public SecurityHeaderLogic(SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.sequenceLogic = sequenceLogic;
        }

        private bool AddUrlToDomains(string url, List<string> domains)
        {
            var splitValue = url.Split('/');
            if (splitValue.Count() > 2)
            {
                var domain = splitValue[2].ToLower();
                if (!domains.Contains(domain))
                {
                    domains.Add(domain);
                    return true;
                }
            }
            return false;
        }

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
            AddUrlToDomains(url, domains);
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
                    AddUrlToDomains(url, domains);
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
                AddUrlToDomains(url, domains);
            }
            HttpContext.Items[Constants.SecurityHeader.FrameAllowIframeOnDomains] = domains;
        }
    }
}
