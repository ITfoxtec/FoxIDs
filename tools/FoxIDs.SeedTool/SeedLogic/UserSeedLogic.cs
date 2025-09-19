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
        private const int uploadUsersWithPasswordBlockSize = 100;
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
            var stop = false;
            var users = new List<UserSeedItem>();
            var lineNumber = 0;
            var totalUsers = 0;
            var successUsers = 0;
            var failedUsers = 0;

            using (var streamReader = File.OpenText(settings.UsersSvcPath))
            {
                while (streamReader.Peek() >= 0)
                {
                    var line = streamReader.ReadLine();
                    lineNumber++;

                    if (line == null)
                    {
                        break;
                    }

                    var items = GetItems(line).ToList();

                    if (firstLine)
                    {
                        headers.AddRange(items);
                        if (headers.Count < 2)
                        {
                            throw new Exception("At least two headers is required.");
                        }
                        var duplicatedHeader = headers.GroupBy(h => h).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                        if (duplicatedHeader != null)
                        {
                            throw new ValidationException($"Duplicated header '{duplicatedHeader}'");
                        }
                        firstLine = false;
                    }
                    else
                    {
                        totalUsers++;

                        if (headers.Count != items.Count)
                        {
                            failedUsers++;
                            LogLineIssue(lineNumber, GetIdentifier(headers, items), "Not the same number of elements in the line as headers.");
                            continue;
                        }

                        try
                        {
                            var user = GetCreateUserApiModel(headers, items);
                            var identifier = GetUserIdentifier(user) ?? GetIdentifier(headers, items);
                            users.Add(new UserSeedItem(user, lineNumber, identifier));

                            if (maxUserToUpload > 0 && users.Count >= maxUserToUpload)
                            {
                                var uploadResult = await UploadAsync(users);
                                successUsers += uploadResult.SuccessCount;
                                failedUsers += uploadResult.FailureCount;
                                Console.WriteLine($"Users uploaded so far: {successUsers} (processed lines: {totalUsers}).");
                                stop = true;
                                break;
                            }

                            if ((!calculatePasswordHash && users.Count(u => !u.User.Password.IsNullOrWhiteSpace()) >= uploadUsersWithPasswordBlockSize) || users.Count >= uploadUsersBlockSize)
                            {
                                var uploadResult = await UploadAsync(users);
                                successUsers += uploadResult.SuccessCount;
                                failedUsers += uploadResult.FailureCount;
                                Console.WriteLine($"Users uploaded so far: {successUsers} (processed lines: {totalUsers}).");
                                users = new List<UserSeedItem>();
                            }
                        }
                        catch (Exception ex)
                        {
                            failedUsers++;
                            LogLineIssue(lineNumber, GetIdentifier(headers, items), ex.GetBaseException().Message);
                        }
                    }
                }

                if (!stop && users.Count > 0)
                {
                    var uploadResult = await UploadAsync(users);
                    successUsers += uploadResult.SuccessCount;
                    failedUsers += uploadResult.FailureCount;
                    Console.WriteLine($"Users uploaded so far: {successUsers} (processed lines: {totalUsers}).");
                }
            }

            Console.WriteLine($"{Environment.NewLine}Upload complete. Total users: {totalUsers}. Uploaded: {successUsers}. Not uploaded: {failedUsers}.");
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


        private static string GetUserIdentifier(CreateUserApiModel user)
        {
            if (user == null)
            {
                return null;
            }

            var identifiers = new List<string>();
            if (!user.Email.IsNullOrWhiteSpace())
            {
                identifiers.Add($"Email: {user.Email}");
            }
            if (!user.Username.IsNullOrWhiteSpace())
            {
                identifiers.Add($"Username: {user.Username}");
            }
            if (!user.Phone.IsNullOrWhiteSpace())
            {
                identifiers.Add($"Phone: {user.Phone}");
            }

            if (identifiers.Count == 0)
            {
                return null;
            }

            return string.Join(", ", identifiers);
        }

        private static string GetIdentifier(List<string> headers, List<string> items)
        {
            if (headers == null || items == null)
            {
                return null;
            }

            var candidates = new[] { "Email", "Username", "Phone" };
            foreach (var candidate in candidates)
            {
                var index = headers.FindIndex(h => string.Equals(h, candidate, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index < items.Count)
                {
                    var value = items[index];
                    if (!value.IsNullOrWhiteSpace())
                    {
                        return $"{candidate}: {value}";
                    }
                }
            }

            return null;
        }

        private static int GetCsvDataLineNumber(int lineNumber)
        {
            if (lineNumber <= 1)
            {
                return 1;
            }

            return lineNumber - 1;
        }


        private static void LogLineIssue(int lineNumber, string identifier, string message)
        {
            var displayLineNumber = GetCsvDataLineNumber(lineNumber);
            var identifierText = string.IsNullOrWhiteSpace(identifier) ? string.Empty : $" ({identifier})";
            Console.WriteLine($"Line {displayLineNumber}{identifierText}: {message}");
        }

        private static void LogBatchFailure(List<UserSeedItem> userItems, Exception exception)
        {
            if (userItems == null || userItems.Count == 0)
            {
                Console.WriteLine("Batch upload failed. Retrying individually.");
                return;
            }

            var firstLine = GetCsvDataLineNumber(userItems.Min(u => u.LineNumber));
            var lastLine = GetCsvDataLineNumber(userItems.Max(u => u.LineNumber));
            var rangeText = firstLine == lastLine ? $"line {firstLine}" : $"lines {firstLine}-{lastLine}";
            Console.WriteLine($"Batch upload failed for {rangeText}. Retrying individually.");
        }

        private async Task<List<CreateUserApiModel>> PasswordToHashPassword(List<CreateUserApiModel> users)
        {
            if (calculatePasswordHash)
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
            }

            return users;
        }

        private async Task<UploadResult> UploadAsync(List<UserSeedItem> userItems)
        {
            if (userItems == null || userItems.Count == 0)
            {
                return new UploadResult(0, 0);
            }

            var accessToken = await accessLogic.GetAccessTokenAsync();
            var usersToUpload = userItems.Select(u => u.User).ToList();

            try
            {
                var preparedUsers = await PasswordToHashPassword(usersToUpload);
                await SavePasswordsRiskListAsync(accessToken, preparedUsers);
                return new UploadResult(userItems.Count, 0);
            }
            catch (Exception ex)
            {
                LogBatchFailure(userItems, ex);
                return await UploadUsersOneByOneAsync(accessToken, userItems);
            }
        }

        private async Task<UploadResult> UploadUsersOneByOneAsync(string accessToken, List<UserSeedItem> userItems)
        {
            var successCount = 0;
            var failureCount = 0;
            var dotPrinted = false;

            foreach (var item in userItems)
            {
                try
                {
                    var singleUser = new List<CreateUserApiModel> { item.User };
                    var preparedUser = await PasswordToHashPassword(singleUser);
                    await SavePasswordsRiskListAsync(accessToken, preparedUser);
                    successCount++;
                    if (successCount % 100 == 0)
                    {
                        Console.Write('.');
                        dotPrinted = true;
                        dotPrinted = true;
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    if (dotPrinted)
                    {
                        Console.WriteLine();
                        dotPrinted = false;
                    }
                    LogLineIssue(item.LineNumber, item.Identifier, $"Upload failed. {ex.GetBaseException().Message}");
                }
            }

            if (dotPrinted)
            {
                Console.WriteLine();
            }

            return new UploadResult(successCount, failureCount);
        }

        private sealed class UserSeedItem
        {
            public UserSeedItem(CreateUserApiModel user, int lineNumber, string identifier)
            {
                User = user;
                LineNumber = lineNumber;
                Identifier = identifier;
            }

            public CreateUserApiModel User { get; }

            public int LineNumber { get; }

            public string Identifier { get; }
        }

        private sealed class UploadResult
        {
            public UploadResult(int successCount, int failureCount)
            {
                SuccessCount = successCount;
                FailureCount = failureCount;
            }

            public int SuccessCount { get; }

            public int FailureCount { get; }
        }
        private class UserHash : ISecretHash
        {
            public string HashAlgorithm { get; set; }
            public string Hash { get; set; }
            public string HashSalt { get; set; }
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

