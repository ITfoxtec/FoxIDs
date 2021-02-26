using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OAuthResourceScopeDownLogic<TClient, TScope, TClaim> : LogicBase where TClient : OAuthDownClient<TScope, TClaim> where TScope : OAuthDownScope<TClaim> where TClaim : OAuthDownClaim
    {
        private readonly TelemetryScopedLogger logger;

        public OAuthResourceScopeDownLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public List<string> GetResourceScopes(TClient client)
        {
            var scopes = new List<string>();

            if (client.ResourceScopes != null)
            {
                foreach (var resourceScope in client.ResourceScopes)
                {
                    scopes.Add(resourceScope.Resource);
                    if (resourceScope.Scopes?.Count() > 0)
                    {
                        scopes.AddRange(resourceScope.Scopes.Select(s => $"{resourceScope.Resource}:{s}"));
                    }
                }
            }

            return scopes;
        }

        public Task<(List<string>, List<string>)> GetValidResourceAsync(TClient client, IEnumerable<string> selectedScopes)
        {
            var selectedResource = new List<string>();
            var selectedResourceScopes = new List<string>();

            if (client.ResourceScopes != null && selectedScopes?.Count() > 0)
            {
                foreach (var resourceScope in client.ResourceScopes)
                {
                    (var subSelectedResource, var subSelectedResourceScopes) = GetDividedResourceAndScopes(resourceScope, selectedScopes);
                    if (!subSelectedResource.IsNullOrEmpty())
                    {
                        selectedResource.Add(resourceScope.Resource);
                        selectedResourceScopes.AddRange(subSelectedResourceScopes);
                    }
                }
            }

            return Task.FromResult((selectedResource, selectedResourceScopes));
        }

        private (string, List<string>) GetDividedResourceAndScopes(OAuthDownResourceScope resourceScope, IEnumerable<string> selectedScopes)
        {
            var selectedResourceScopes = new List<string>();
            if(resourceScope.Scopes != null)
            {
                selectedResourceScopes.AddRange(resourceScope.Scopes.Select(s => $"{resourceScope.Resource}:{s}").Where(s => selectedScopes.Contains(s)));
            }

            var resourceScopeMatch = selectedResourceScopes.Count() > 0 || selectedScopes.Contains(resourceScope.Resource);
            return (resourceScopeMatch ? resourceScope.Resource : null, selectedResourceScopes);
        }
    }
}
