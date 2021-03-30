using FoxIDs.Models.Sequences;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SecurityHeaderLogic : LogicBase
    {
        private readonly SequenceLogic sequenceLogic;

        public SecurityHeaderLogic(SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.sequenceLogic = sequenceLogic;
        }

        // TODO consider removing the FormActionSequenceData...

        //public async Task CreateFormActionByUrlAsync(string url)
        //{
        //    var domains = new List<string>();
        //    if (!url.IsNullOrEmpty())
        //    {
        //        AddUrlToDomains(url, domains);
        //    }
        //    await sequenceLogic.SaveSequenceDataAsync(new FormActionSequenceData
        //    {
        //        Domains = domains
        //    });
        //}

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

        //public async Task AddFormActionByUrlAsync(string url)
        //{
        //    if (!url.IsNullOrEmpty())
        //    {
        //        var formActionSequenceData = await sequenceLogic.GetSequenceDataAsync<FormActionSequenceData>(remove: false);
        //        if (AddUrlToDomains(url, formActionSequenceData.Domains))
        //        {
        //            await sequenceLogic.SaveSequenceDataAsync(formActionSequenceData);
        //        }
        //    }
        //}

        public void AddFormActionAllowAll()
        {
            HttpContext.Items[Constants.SecurityHeader.FormActionsDomainsAllowAll] = true;
        }

        public async Task<List<string>> GetFormActionDomainsAsync()
        {
            var formActionSequenceData = await sequenceLogic.GetSequenceDataAsync<FormActionSequenceData>(remove: false, allowNull: true);
            if (formActionSequenceData?.Domains?.Count() > 0)
            {
                return AddAllowAll(formActionSequenceData.Domains);
            }
            else if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FormActionDomains))
            {
                return AddAllowAll(HttpContext.Items[Constants.SecurityHeader.FormActionDomains] as List<string>);
            }
            else
            {
                return AddAllowAll(null);
            }
        }

        private List<string> AddAllowAll(List<string> domains)
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FormActionsDomainsAllowAll))
            {
                domains = domains ?? new List<string>();
                domains.Add("*");
            }
            return domains;
        }

        public async Task RemoveFormActionSequenceDataAsync(string addUrl = null)
        {
            var formActionSequenceData = await sequenceLogic.GetSequenceDataAsync<FormActionSequenceData>(remove: true, allowNull: true);
            if (formActionSequenceData?.Domains?.Count() > 0 || !addUrl.IsNullOrEmpty())
            {
                var domains = formActionSequenceData?.Domains?.Count() > 0 ? new List<string>(formActionSequenceData?.Domains) : new List<string>();
                if (!addUrl.IsNullOrEmpty())
                {
                    AddUrlToDomains(addUrl, domains);
                }

                HttpContext.Items[Constants.SecurityHeader.FormActionDomains] = domains;
            }
        }

        public List<string> GetFrameSrcDomains()
        {
            if (HttpContext.Items.ContainsKey(Constants.SecurityHeader.FrameSrcDomains))
            {
                return AddAllowAll(HttpContext.Items[Constants.SecurityHeader.FrameSrcDomains] as List<string>);
            }
            else
            {
                return null;
            }
        }

        public void AddFrameSrc(List<string> urls)
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
    }
}
