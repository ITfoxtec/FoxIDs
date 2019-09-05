using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Infrastructure.Swagger
{
    /// <summary>
    /// Support conventional routing.
    /// From ApiDescriptionGroupCollectionProvider https://github.com/aspnet/AspNetCore/blob/7d4258b18616579773836d89da25f039b6dae7f1/src/Mvc/Mvc.ApiExplorer/src/ApiDescriptionGroupCollectionProvider.cs
    /// </summary>
    public class FoxIDsApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider
    {
        private readonly IActionDescriptorCollectionProvider actionDescriptorCollectionProvider;
        private readonly IApiDescriptionProvider[] apiDescriptionProviders;

        private ApiDescriptionGroupCollection apiDescriptionGroups;

        /// <summary>
        /// Creates a new instance of <see cref="FoxIDsApiDescriptionGroupCollectionProvider"/>.
        /// </summary>
        /// <param name="actionDescriptorCollectionProvider">
        /// The <see cref="IActionDescriptorCollectionProvider"/>.
        /// </param>
        /// <param name="apiDescriptionProviders">
        /// The <see cref="IEnumerable{IApiDescriptionProvider}"/>.
        /// </param>
        public FoxIDsApiDescriptionGroupCollectionProvider(
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
            IEnumerable<IApiDescriptionProvider> apiDescriptionProviders)
        {
            this.actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            this.apiDescriptionProviders = apiDescriptionProviders.OrderBy(item => item.Order).ToArray();
        }

        /// <inheritdoc />
        public ApiDescriptionGroupCollection ApiDescriptionGroups
        {
            get
            {
                var actionDescriptors = actionDescriptorCollectionProvider.ActionDescriptors;
                if (apiDescriptionGroups == null || apiDescriptionGroups.Version != actionDescriptors.Version)
                {
                    apiDescriptionGroups = GetCollection(actionDescriptors);
                }

                return apiDescriptionGroups;
            }
        }

        private ApiDescriptionGroupCollection GetCollection(ActionDescriptorCollection actionDescriptors)
        {
            var items = new List<ActionDescriptor>(actionDescriptors.Items);

            foreach (var action in items.OfType<ControllerActionDescriptor>())
            {
                var httpMethods = GetHttpMethods(action);
                if (httpMethods != null)
                {
                    action.AttributeRouteInfo = new Microsoft.AspNetCore.Mvc.Routing.AttributeRouteInfo { Template = GetTemplate(action.ControllerName) };
                    action.SetProperty(new ApiDescriptionActionData { GroupName = Constants.Api.Version });
                }
            }

            var context = new ApiDescriptionProviderContext(items.AsReadOnly());

            foreach (var provider in apiDescriptionProviders)
            {
                provider.OnProvidersExecuting(context);
            }

            for (var i = apiDescriptionProviders.Length - 1; i >= 0; i--)
            {
                apiDescriptionProviders[i].OnProvidersExecuted(context);
            }

            var groups = context.Results
                .GroupBy(d => d.GroupName)
                .Select(g => new ApiDescriptionGroup(g.Key, g.ToArray()))
                .ToArray();

            return new ApiDescriptionGroupCollection(groups, actionDescriptors.Version);
        }

        private string GetTemplate(string controllerName)
        {
            controllerName = controllerName.ToLower();
            if (controllerName.StartsWith(Constants.Routes.ApiControllerPreMasterKey))
            {
                return $"{Constants.Routes.MasterApiName}/{Constants.Routes.PreApikey}{controllerName.Substring(1)}";
            }
            else if (controllerName.StartsWith(Constants.Routes.ApiControllerPreTenantTrackKey))
            {
                return $"[tenant_name]/[track_name]/{Constants.Routes.PreApikey}{controllerName.Substring(1)}";
            }
            else
            {
                throw new NotSupportedException("Only master and tenant controller supported.");
            }
        }

        private IEnumerable<string> GetHttpMethods(ControllerActionDescriptor action)
        {
            if (action.ActionConstraints != null && action.ActionConstraints.Count > 0)
            {
                return action.ActionConstraints.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods);
            }
            else
            {
                return null;
            }
        }
    }
}