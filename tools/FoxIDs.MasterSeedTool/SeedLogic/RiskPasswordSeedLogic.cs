using FoxIDs.MasterSeedTool.Logic;
using FoxIDs.MasterSeedTool.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
using FoxIDs.MasterSeedTool.Models.ApiModels;

namespace FoxIDs.MasterSeedTool.SeedLogic
{
    public class RiskPasswordSeedLogic
    {
        private const int maxRiskPasswordToUpload = 0; // 0 is unlimited
        private const int riskPasswordMoreThenBreachesCount = 100; 
        private const int uploadRiskPasswordBlockSize = 1000; 
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
        public string PasswordRiskFirstListApiEndpoint => UrlCombine.Combine(settings.FoxIDsMasterControlApiEndpoint, "!riskpasswordfirst");

        public async Task SeedAsync()
        {
            Console.WriteLine("**Uploading risk passwords**");
            var addCount = 0;
            var readCount = 0;
            var stop = false;
            var riskPasswords = new List<RiskPasswordApiModel>();            
            using (var streamReader = File.OpenText(settings.PwnedPasswordsPath))
            {
                while (streamReader.Peek() >= 0)
                {
                    var split = streamReader.ReadLine().Split(':');
                    var breachesCount = Convert.ToInt32(split[1]);
                    readCount++;
                    if (breachesCount >= riskPasswordMoreThenBreachesCount)
                    {
                        riskPasswords.Add(new RiskPasswordApiModel { PasswordSha1Hash = split[0], Count = breachesCount });
                        addCount++;

                        if (maxRiskPasswordToUpload > 0 && addCount >= maxRiskPasswordToUpload)
                        {
                            await UploadAsync(riskPasswords, readCount, addCount);
                            stop = true;
                            break;
                        }
                        if (riskPasswords.Count() >= uploadRiskPasswordBlockSize)
                        {
                            await UploadAsync(riskPasswords, readCount, addCount);
                            riskPasswords = new List<RiskPasswordApiModel>();
                        }
                    }
                }

                if (!stop && riskPasswords.Count() > 0)
                {
                    await UploadAsync(riskPasswords, readCount, addCount);
                }
            }

            Console.WriteLine($"{Environment.NewLine}Risk passwords total read: {readCount}, and total uploaded: {addCount}");
        }

        private async Task UploadAsync(List<RiskPasswordApiModel> riskPasswords, int readCount, int addCount)
        {
            await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
            Console.WriteLine($"Risk passwords read: {readCount}, and uploaded: {addCount}");
        }

        public async Task DeleteAllAsync()
        {
            Console.WriteLine("**Delete all risk passwords**");
            var totalCount = 0;
            while (true)
            {
                var accessToken = await accessLogic.GetAccessTokenAsync();
                Console.Write("Get risk passwords");
                var riskPasswords = await GetPasswordsRiskFirstListAsync(accessToken);
                if(riskPasswords?.Count > 0)
                {
                    Console.WriteLine($": {riskPasswords?.Count}");
                    totalCount = totalCount + riskPasswords.Count();
                    await DeletePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords.Select(r => r.PasswordSha1Hash).ToList());
                    Console.WriteLine($"Risk passwords deleted: {totalCount}");
                }
                else
                {
                    Console.WriteLine(": 0");
                    break;
                }
            }

            Console.WriteLine($"{Environment.NewLine}All {totalCount} risk passwords deleted");
        }

        private async Task SavePasswordsRiskListAsync(string accessToken, List<RiskPasswordApiModel> riskPasswords)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.UpdateJsonAsync(PasswordRiskListApiEndpoint, new RiskPasswordRequestApiModel { RiskPasswords = riskPasswords });
            await response.ValidateResponseAsync();
        }

        private async Task<List<RiskPasswordApiModel>> GetPasswordsRiskFirstListAsync(string accessToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.GetAsync(PasswordRiskFirstListApiEndpoint);
            await response.ValidateResponseAsync();
            var result = await response.Content.ReadAsStringAsync();
            return result.ToObject<List<RiskPasswordApiModel>>();   
        }

        private async Task DeletePasswordsRiskListAsync(string accessToken, List<string> passwordSha1Hashs)
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);

            var body = new RiskPasswordDeleteApiModel { PasswordSha1Hashs = passwordSha1Hashs };
            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = content;

            request.Method = new HttpMethod("DELETE");
            request.RequestUri = new Uri(PasswordRiskListApiEndpoint);
            var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            await response.ValidateResponseAsync();
        }
    }
}
