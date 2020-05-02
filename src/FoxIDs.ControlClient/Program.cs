using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;

namespace FoxIDs.ControlClient
{
    public class Program
    {
        const string httpClientLogicalName = "FoxIDs.API";

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddHttpClient(httpClientLogicalName, client => client.BaseAddress = new Uri(builder.Configuration["AppSettings:FoxIDsApiUrl"]))
                .AddHttpMessageHandler(sp =>
                {
                    var handler = sp.GetService<AuthorizationMessageHandler>()
                        .ConfigureHandler(
                            authorizedUrls: new[] { builder.Configuration["AppSettings:FoxIDsApiUrl"] },
                            scopes: new[] { builder.Configuration["AppSettings:FoxIDsApiScope"] });
                    return handler;
                });

            builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientLogicalName));

            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("IdentitySettings", options.ProviderOptions);
                options.ProviderOptions.DefaultScopes.Add(builder.Configuration["AppSettings:FoxIDsApiScope"]);
                options.ProviderOptions.ResponseType = "code";
            });

            await builder.Build().RunAsync();
        }
    }
}
