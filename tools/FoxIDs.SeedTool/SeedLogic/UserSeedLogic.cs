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
using FoxIDs.Logic;
using FoxIDs.Models;
using System.Threading;

namespace FoxIDs.SeedTool.SeedLogic
{
    public class UserSeedLogic
    {
        private const int maxUserToUpload = 0; // 0 is unlimited
        private const int uploadUsersWithPassowrdBlockSize = 100;
        private const int uploadUsersBlockSize = 1000;
        private const bool calculatePasswordHash = true; 
        private readonly SeedSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AccessLogic accessLogic;
        private readonly SecretHashLogic secretHashLogic;

        public UserSeedLogic(SeedSettings settings, IHttpClientFactory httpClientFactory, AccessLogic accessLogic, SecretHashLogic secretHashLogic)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.accessLogic = accessLogic;
            this.secretHashLogic = secretHashLogic;
        }

        public string UsersApiEndpoint => UrlCombine.Combine(settings.FoxIDsControlApiEndpoint, "!users");

        public async Task SeedAsync()
        {
            Console.WriteLine("**Upload users**");
            var headers = new List<string>();
            var firstLine = true;
            var addCount = 0;
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
                        if (headers.Count() != items.Count())
                        {
                            throw new Exception("Not the same number of elements in the line as headers.");
                        }

                        users.Add(GetCreateUserApiModel(headers, items));
                        addCount++;

                        if (maxUserToUpload > 0 && users.Count() >= maxUserToUpload)
                        {
                            await UploadAsync(users, addCount);
                            stop = true;
                            break;
                        }
                        if ((!calculatePasswordHash && users.Where(u => !u.Password.IsNullOrWhiteSpace()).Count() >= uploadUsersWithPassowrdBlockSize) || users.Count() >= uploadUsersBlockSize)
                        {
                            await UploadAsync(users, addCount);
                            users = new List<CreateUserApiModel>();
                        }
                    }
                }

                if (!stop && users.Count() > 0)
                {
                    await UploadAsync(users, addCount);
                }
            }

            Console.WriteLine($"{Environment.NewLine}Total uploaded users: {addCount}.");
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

        private async Task<List<CreateUserApiModel>> PasswordToHashPassword(List<CreateUserApiModel> users)
        {
            var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount));
            var tasks = new List<Task>();
            int hashedCount = 0; 

            foreach (var user in users)
            {
                if (!user.Password.IsNullOrWhiteSpace())
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            var userHash = new UserHash();
                            await secretHashLogic.AddSecretHashAsync(userHash, user.Password).ConfigureAwait(false);
                            user.PasswordHashAlgorithm = userHash.HashAlgorithm;
                            user.PasswordHash = userHash.Hash;
                            user.PasswordHashSalt = userHash.HashSalt;
                            user.Password = null; // Clear password after hashing

                            var current = Interlocked.Increment(ref hashedCount);
                            if (current % 10 == 0) Console.Write('.'); 
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            if (hashedCount >= 10) Console.WriteLine(); 
            return users;
        }

        private class UserHash : ISecretHash
        {
            public string HashAlgorithm { get; set; }
            public string Hash { get; set; }
            public string HashSalt { get; set; }
        }

        private async Task UploadAsync(List<CreateUserApiModel> users, int addCount)
        {
            var accessToken = await accessLogic.GetAccessTokenAsync();
            await SavePasswordsRiskListAsync(accessToken, await PasswordToHashPassword(users));
            Console.WriteLine($"Users uploaded: {addCount}");
        }

        public async Task DeleteAllAsync()
        {
            Console.WriteLine("**Delete all users**");
            var totalCount = 0;
            while (true)
            {
                var accessToken = await accessLogic.GetAccessTokenAsync();
                Console.Write("Get users");
                var usersResult = await GetUserIdentifiersFirstListAsync(accessToken);
                if(usersResult?.Data?.Count > 0)
                {
                    Console.WriteLine($": {usersResult?.Data?.Count}");
                    totalCount = totalCount + usersResult.Data.Count();
                    await DeletePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), GetUserIdentifiers(usersResult.Data).ToList());
                    Console.WriteLine($"Users deleted: {totalCount}");
                }
                else
                {
                    Console.WriteLine(": 0");
                    break;
                }
            }

            Console.WriteLine($"{Environment.NewLine}All {totalCount} users have been deleted");
        }

        private IEnumerable<string> GetUserIdentifiers(HashSet<UserApiModel> users)
        {
            foreach (var user in users)
            {
                if (!user.Email.IsNullOrWhiteSpace())
                {
                    yield return user.Email;
                }
                else if (!user.Phone.IsNullOrWhiteSpace())
                {
                    yield return user.Phone;
                }
                else if (!user.Username.IsNullOrWhiteSpace())
                {
                    yield return user.Username;
                }
                else 
                {
                    throw new InvalidDataException($"Invalid user '{user.ToJson()}'.");
                }
            }
        }

        private async Task SavePasswordsRiskListAsync(string accessToken, List<CreateUserApiModel> users)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.UpdateJsonAsync(UsersApiEndpoint, new UsersRequestApiModel { Users = users });
            await response.ValidateResponseAsync();
        }

        private async Task<PaginationResponse<UserApiModel>> GetUserIdentifiersFirstListAsync(string accessToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.GetAsync(UsersApiEndpoint);
            await response.ValidateResponseAsync();
            var result = await response.Content.ReadAsStringAsync();
            return result.ToObject<PaginationResponse<UserApiModel>>();   
        }

        private async Task DeletePasswordsRiskListAsync(string accessToken, List<string> userIdentifiers)
        {
            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);

            var body = new UsersDeleteApiModel { UserIdentifiers = userIdentifiers };
            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = content;

            request.Method = new HttpMethod("DELETE");
            request.RequestUri = new Uri(UsersApiEndpoint);
            var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            await response.ValidateResponseAsync();
        }
    }
}
