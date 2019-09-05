using FoxIDs.SeedTool.Logic;
using FoxIDs.SeedTool.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.SeedTool.SeedLogic
{
    public class PasswordRiskListSeedLogic
    {
        private readonly SeedSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AccessLogic accessLogic;

        public PasswordRiskListSeedLogic(SeedSettings settings, IHttpClientFactory httpClientFactory, AccessLogic accessLogic)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.accessLogic = accessLogic;
        }

        public string PasswordRiskListApiEndpoint => UrlCombine.Combine(settings.FoxIDsMasterApiEndpoint, "!passwordrisklist");

        public async Task SeedAsync()
        {
            Console.WriteLine("Creating passwords risk list");
            var riskPasswords = new List<RiskPasswordItemApiModel>();            
            using (var streamReader = File.OpenText(settings.PwnedPasswordsPath))
            {
                var i = 0;
                while (streamReader.Peek() >= 0)
                {
                    i++;
                    var split = streamReader.ReadLine().Split(':');
                    var passwordCount = Convert.ToInt32(split[1]);
                    if (passwordCount >= 100)
                    {
                        riskPasswords.Add(new RiskPasswordItemApiModel { PasswordSha1Hash = split[0], Count = passwordCount });
                        if (riskPasswords.Count == 1000)
                        {
                            Console.WriteLine($"Saving risk passwords, current password count '{passwordCount}'");
                            await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
                            riskPasswords = new List<RiskPasswordItemApiModel>();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (riskPasswords.Count > 0)
            {
                Console.WriteLine("Saving the last risk passwords");
                await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
            }

            Console.WriteLine("Risk passwords seeded");
        }

        private async Task SavePasswordsRiskListAsync(string accessToken, List<RiskPasswordItemApiModel> riskPasswords)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using (var response = await client.PostJsonAsync(PasswordRiskListApiEndpoint, new RiskPasswordApiModel { RiskPasswords = riskPasswords }))
            {
                await response.ValidateResponseAsync();
            }
        }
    }
}
