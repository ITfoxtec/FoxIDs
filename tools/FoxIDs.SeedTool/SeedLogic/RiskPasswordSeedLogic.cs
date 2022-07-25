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
    public class RiskPasswordSeedLogic
    {
        private const int riskPasswordMoreThenBreachesCount = 100;
        private const int uploadRiskPasswordBlokSize = 10000;
        private readonly SeedSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AccessLogic accessLogic;

        public RiskPasswordSeedLogic(SeedSettings settings, IHttpClientFactory httpClientFactory, AccessLogic accessLogic)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.accessLogic = accessLogic;
        }

        public string PasswordRiskListApiEndpoint => UrlCombine.Combine(settings.FoxIDsMasterControlApiEndpoint, "!riskpassword");

        public async Task SeedAsync()
        {
            Console.WriteLine("Uploading risk passwords");
            var totalCount = 0;
            var riskPasswords = new List<RiskPasswordApiModel>();            
            using (var streamReader = File.OpenText(settings.PwnedPasswordsPath))
            {
                var i = 0;
                while (streamReader.Peek() >= 0)
                {
                    i++;
                    var split = streamReader.ReadLine().Split(':');
                    var breachesCount = Convert.ToInt32(split[1]);
                    if (breachesCount >= riskPasswordMoreThenBreachesCount)
                    {
                        riskPasswords.Add(new RiskPasswordApiModel { PasswordSha1Hash = split[0], Count = breachesCount });
                        if (riskPasswords.Count >= uploadRiskPasswordBlokSize)
                        {
                            totalCount += riskPasswords.Count;
                            await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
                            Console.WriteLine($"Risk passwords uploaded '{totalCount}', last breaches count '{breachesCount}'");
                            riskPasswords = new List<RiskPasswordApiModel>();
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
                Console.WriteLine("Uploading the last risk passwords");
                await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
            }

            Console.WriteLine($"All '{totalCount}' risk passwords uploaded");
        }

        private async Task SavePasswordsRiskListAsync(string accessToken, List<RiskPasswordApiModel> riskPasswords)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await client.UpdateJsonAsync(PasswordRiskListApiEndpoint, new RiskPasswordRequestApiModel { RiskPasswords = riskPasswords });
            await response.ValidateResponseAsync();
        }
    }
}
