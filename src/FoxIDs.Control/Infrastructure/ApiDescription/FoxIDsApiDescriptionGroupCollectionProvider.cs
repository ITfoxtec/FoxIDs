using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FoxIDs.Infrastructure.ApiDescription
{
    /// <summary>
    /// Support conventional routing.
    /// From ApiDescriptionGroupCollectionProvider 
    ///     https://github.com/aspnet/AspNetCore/blob/7d4258b18616579773836d89da25f039b6dae7f1/src/Mvc/Mvc.ApiExplorer/src/ApiDescriptionGroupCollectionProvider.cs
    ///     https://github.com/aspnet/AspNetCore/blob/c565386a3ed135560bc2e9017aa54a950b4e35dd/src/Mvc/Mvc.ApiExplorer/src/ApiDescriptionGroupCollectionProvider.cs
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
        public FoxIDsApiDescriptionGroupCollectionProvider(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, IEnumerable<IApiDescriptionProvider> apiDescriptionProviders)
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
                    action.ActionConstraints = new[] { new HttpMethodActionConstraint(httpMethods) };
                    action.AttributeRouteInfo = new Microsoft.AspNetCore.Mvc.Routing.AttributeRouteInfo { Name = action.ActionName, Template = GetTemplate(action.ControllerName) };

                    //TODO Work around. When solved change back to use ApiDescriptionActionData.
                    //action.SetProperty(new ApiDescriptionActionData { GroupName = Constants.Api.Version });
                    SetApiDescriptionActionData(action, Constants.ControlApi.Version);
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

        //TODO Work around. ApiDescriptionActionData is changed from public to intern in ASP.NET Core 3.0.
        // https://github.com/aspnet/AspNetCore/issues/14954
        // https://github.com/tebeco/AspNetCore/commit/8e1e40d8cce6abfca66b14f30161fd36615f89fb
        // https://github.com/aspnet/AspNetCore/blob/7d4258b18616579773836d89da25f039b6dae7f1/src/Mvc/Mvc.ApiExplorer/src/DefaultApiDescriptionProvider.cs
        private void SetApiDescriptionActionData(ControllerActionDescriptor action, string groupName)
        {
            var type = typeof(IApiRequestMetadataProvider).Assembly.GetType("Microsoft.AspNetCore.Mvc.ApiDescriptionActionData");
            var apiDescriptionActionData = Activator.CreateInstance(type, true);

            PropertyInfo prop = type.GetProperty("GroupName");
            prop.SetValue(apiDescriptionActionData, groupName, null);

            action.Properties[type] = apiDescriptionActionData;
        }

        private string GetTemplate(string controllerName)
        {
            if (controllerName.StartsWith(Constants.Routes.ApiControllerPreMasterKey, StringComparison.OrdinalIgnoreCase))
            {
                return $"{Constants.Routes.MasterApiName}/{Constants.Routes.PreApikey}{controllerName.Substring(1)}";
            }
            else if (controllerName.StartsWith(Constants.Routes.ApiControllerPreTenantTrackKey, StringComparison.OrdinalIgnoreCase))
            {
                return $"{{tenant_name}}/{{track_name}}/{Constants.Routes.PreApikey}{controllerName.Substring(1)}";
            }
            else
            {
                throw new NotSupportedException("Only master and tenant controller supported.");
            }
        }

        private IEnumerable<string> GetHttpMethods(ControllerActionDescriptor action)
        {
            var httpMethods = Constants.ControlApi.SupportedApiHttpMethods.Where(m => action.ActionName.StartsWith(m, StringComparison.OrdinalIgnoreCase)).ToList();
            if (httpMethods.Any())
            {
                return httpMethods;
            }
            else
            {
                return null;
            }
        }
    }
}