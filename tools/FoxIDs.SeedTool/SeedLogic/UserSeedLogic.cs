using FoxIDs.SeedTool.Logic;
using FoxIDs.SeedTool.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
using FoxIDs.SeedTool.Models.ApiModels;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.SeedTool.SeedLogic
{
    public class UserSeedLogic
    {
        private const int maxUserToUpload = 0; // 1000000; // 0 is unlimited
        private const int uploadUserBlockSize = 10000; // 1000;
        private readonly SeedSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AccessLogic accessLogic;

        public UserSeedLogic(SeedSettings settings, IHttpClientFactory httpClientFactory, AccessLogic accessLogic)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.accessLogic = accessLogic;
        }

        public string UsersApiEndpoint => UrlCombine.Combine(settings.FoxIDsControlApiEndpoint, "!users");

        public async Task SeedAsync()
        {
            Console.Write("Uploading users");
            var headers = new List<string>();
            var firstLine = true;
            var addCount = 0;
            var readCount = 0;
            var stop = false;
            var users = new List<CreateUserApiModel>();            
            using (var streamReader = File.OpenText(settings.UsersSvcPath))
            {
                while (streamReader.Peek() >= 0)
                {
                    var items = GetItems(streamReader.ReadLine());
                    if (firstLine)
                    {
                        headers.AddRange(items);
                        if (headers.Count() < 2)
                        {
                            throw new Exception("At least two headers is required.");
                        }
                        var duplicatedHeader = headers.GroupBy(h => h).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                        if (headers.GroupBy(h => h).Where(g => g.Count() > 1).Any())
                        {
                            throw new ValidationException($"Duplicated header '{duplicatedHeader}'");
                        }
                        firstLine = false;
                    }
                    else
                    {
                        readCount++;
                        if (headers.Count() != items.Count())
                        {
                            throw new Exception("Not the same number of elements in the line as headers.");
                        }

                        users.Add(GetCreateUserApiModel(headers, items));
                        addCount++;
                        if (addCount % 1000 == 0)
                        {
                            Console.Write($"{Environment.NewLine}Users read '{readCount}'");
                        }
                        else if (addCount % 100 == 0)
                        {
                            Console.Write(".");
                        }

                        if (maxUserToUpload > 0 && addCount >= maxUserToUpload)
                        {
                            await UploadAsync(users);
                            stop = true;
                            break;
                        }
                        if (users.Count() >= uploadUserBlockSize)
                        {
                            await UploadAsync(users);
                            users = new List<CreateUserApiModel>();
                        }
                    }
                }

                if (!stop && users.Count() > 0)
                {
                    await UploadAsync(users);
                }
            }

            Console.WriteLine($"{Environment.NewLine}Users total read '{readCount}', total uploaded '{addCount}'");
        }

        private CreateUserApiModel GetCreateUserApiModel(List<string> headers, IEnumerable<string> items)
        {
            var properties = new Dictionary<string, string>();
            var count = 0;
            List<ClaimAndValuesApiModel> claims = null;
            foreach (var item in items)
            {
                var header = headers[count++];
                if (!item.IsNullOrWhiteSpace())
                {
                    if (header == "Claims")
                    {
                        claims = item.ToObject<List<ClaimAndValuesApiModel>>();
                    }
                    else
                    {
                        properties.Add(header, item);
                    }
                }
            }

            var createUserApiModel = properties.ToObject<CreateUserApiModel>();
            createUserApiModel.Claims = claims;
            return createUserApiModel;
        }

        private IEnumerable<string> GetItems(string line)
        {
            var split = line.Split(';');
            foreach (var item in split)
            {
                var itemValue = item.Trim('"');
                itemValue = itemValue.Replace("\"\"", "\"");
                yield return itemValue;
            }
        }

        private async Task UploadAsync(List<CreateUserApiModel> users)
        {
            await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), users);
            Console.WriteLine($"{Environment.NewLine}Users uploaded '{users.Count()}'");            
        }

        public async Task DeleteAllAsync()
        {
            Console.WriteLine("Delete all users");
            var totalCount = 0;
            while (true)
            {
                var users = await GetUserIdentifiersFirstListAsync(await accessLogic.GetAccessTokenAsync());
                if(users?.Count > 0)
                {
                    totalCount = totalCount + users.Count();
                    await DeletePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), GetUserIdentifiers(users).ToList());
                    Console.WriteLine($"Users deleted '{totalCount}'");
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine($"All '{totalCount}' users deleted");
        }

        private IEnumerable<string> GetUserIdentifiers(List<UserApiModel> users)
        {
            foreach (var user in users)
            {
                if (user.Email.IsNullOrWhiteSpace())
                {
                    yield return user.Email;
                }
                else if (user.Phone.IsNullOrWhiteSpace())
                {
                    yield return user.Phone;
                }
                else if (user.Username.IsNullOrWhiteSpace())
                {
                    yield return user.Username;
                }
                else 
                {
                    throw new InvalidDataException($"Invalid user '{user.ToJson()}'.");
                }
            }
        }

        public async Task DeleteAllInPartitionAsync()
        {
            Console.WriteLine("Delete all users");
            await DeletePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync());
            Console.WriteLine("All users deleted");
        }
        private async Task SavePasswordsRiskListAsync(string accessToken, List<CreateUserApiModel> users)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.UpdateJsonAsync(UsersApiEndpoint, new UsersRequestApiModel { Users = users });
            await response.ValidateResponseAsync();
        }

        private async Task<List<UserApiModel>> GetUserIdentifiersFirstListAsync(string accessToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.GetAsync(UsersApiEndpoint);
            await response.ValidateResponseAsync();
            var result = await response.Content.ReadAsStringAsync();
            return result.ToObject<List<UserApiModel>>();   
        }

        private async Task DeletePasswordsRiskListAsync(string accessToken, List<string> userIdentifiers = null)
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            if (userIdentifiers != null)
            {
                var body = new UsersDeleteApiModel { UserIdentifiers = userIdentifiers };
                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body));
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                request.Content = content;
            }
            request.Method = new HttpMethod("DELETE");
            request.RequestUri = new Uri(UsersApiEndpoint);
            var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            await response.ValidateResponseAsync();
        }
    }
}
