using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using FoxIDs.Models;
using System.Linq;

namespace FoxIDs
{
    public class Program
    {
        const string httpClientLogicalName = "FoxIDs.ControlAPI";

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddHttpClient(httpClientLogicalName, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                       .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientLogicalName));

            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("IdentitySettings", options.ProviderOptions);
                options.ProviderOptions.DefaultScopes.Add("email");
                options.ProviderOptions.DefaultScopes.Add(builder.Configuration["AppSettings:FoxIDsControlApiScope"]);
                options.ProviderOptions.ResponseType = "code";
            });

            builder.Services.AddSingleton(s => new RouteBindingBase
            {
                TenantName = builder.HostEnvironment.BaseAddress.TrimEnd('/').Split('/').Last()
            });

            await builder.Build().RunAsync();
        }
    }
}
