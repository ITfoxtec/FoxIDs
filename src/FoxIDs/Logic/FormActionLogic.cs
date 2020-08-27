using FoxIDs.Infrastructure;
using FoxIDs.Models.Sequences;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class FormActionLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly SequenceLogic sequenceLogic;

        public FormActionLogic(TelemetryScopedLogger logger, SequenceLogic sequenceLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.sequenceLogic = sequenceLogic;
        }

        public async Task CreateFormActionByUrlAsync(string url)
        {
            var domains = new List<string>();
            if (!url.IsNullOrEmpty())
            {
                AddUrlToDomains(url, domains);
            }
            await sequenceLogic.SaveSequenceDataAsync(new FormActionSequenceData
            {
                Domains = domains
            });
        }

        private bool AddUrlToDomains(string url, List<string> domains)
        {
            var splitValue = url.Split('/');
            if (splitValue.Count() > 2)
            {
                var domain = splitValue[2].ToLower();
                if(!domains.Contains(domain))
                {
                    domains.Add(domain);
                    return true;
                }
            }
            return false;
        }

        public async Task AddFormActionByUrlAsync(string url)
        {
            if (!url.IsNullOrEmpty())
            {
                var formActionSequenceData = await sequenceLogic.GetSequenceDataAsync<FormActionSequenceData>(remove: false);
                if (AddUrlToDomains(url, formActionSequenceData.Domains))
                {
                    await sequenceLogic.SaveSequenceDataAsync(formActionSequenceData);
                }
            }
        }

        public async Task<List<string>> GetFormActionDomainsAsync()
        {
            var formActionSequenceData = await sequenceLogic.GetSequenceDataAsync<FormActionSequenceData>(remove: false, allowNull: true);
            if (formActionSequenceData?.Domains?.Count() > 0)
            {
                return formActionSequenceData.Domains;
            }
            else if (HttpContext.Items.ContainsKey(Constants.FormAction.Domains))
            {
                return HttpContext.Items[Constants.FormAction.Domains] as List<string>;
            }
            else
            {
                return null;
            }
        }

        public async Task RemoveFormActionSequenceDataAsync()
        {
            var formActionSequenceData = await sequenceLogic.GetSequenceDataAsync<FormActionSequenceData>(remove: true, allowNull: true);
            if (formActionSequenceData?.Domains?.Count() > 0)
            {
                HttpContext.Items[Constants.FormAction.Domains] = formActionSequenceData.Domains;
            }
        }
    }
}
