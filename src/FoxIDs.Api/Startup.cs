using FoxIDs.Infrastructure.Hosting;
using FoxIDs.Infrastructure.Swagger;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;

namespace FoxIDs
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment CurrentEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var settings = services.BindConfig<FoxIDsApiSettings>(Configuration, nameof(Settings));
            // Also add as Settings
            services.AddSingleton<Settings>(settings);

            services.AddInfrastructure(settings, CurrentEnvironment);
            services.AddRepository();
            services.AddLogic();

            services.AddAuthenticationAndAuthorization(settings);

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddMvcCore(options =>
            {
                options.EnableEndpointRouting = false;
            })
                .AddAuthorization()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonFormatters()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                })
                .AddFoxIDsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(Constants.Api.Version, new Info { Title = "FoxIDs API", Version = Constants.Api.Version });
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                c.TagActionsBy(s => new[]
                {
                    GetTagActionsBy(s.ActionDescriptor as ControllerActionDescriptor)                    
                });

                //c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                //{
                //    { "Bearer", new string[] { } }
                //});
            });
        }

        private string GetTagActionsBy(ControllerActionDescriptor controllerActionDescriptor)
        {
            var controllerName = controllerActionDescriptor.ControllerName.ToLower();
            if (controllerName.StartsWith(Constants.Routes.ApiControllerPreMasterKey))
            {
                return $"master {controllerName.Substring(1)}";
            }
            else if (controllerName.StartsWith(Constants.Routes.ApiControllerPreTenantTrackKey))
            {
                return $"tenant {controllerName.Substring(1)}";
            }
            else
            {
                throw new NotSupportedException("Only master and tenant controller supported.");
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            if (!CurrentEnvironment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseProxyClientIpMiddleware();
            app.UseEnLocalization();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoxIDs API");
            });

            app.UseMvc(routes =>
            {
                routes.Routes.Add(new FoxIDsApiRouter(routes.DefaultHandler));


                routes.MapRoute(
                    name: "default",
                    template: $"{{controller={Constants.Routes.DefaultWebSiteController}}}/{{action={Constants.Routes.DefaultWebSiteAction}}}/{{id?}}");
            });
        }
    }
}
