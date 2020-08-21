using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UserService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!user";
        private const string filterApiUri = "api/{tenant}/master/!filteruser";

        public UserService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(httpClient, routeBindingLogic)
        { }

        public async Task<IEnumerable<User>> FilterUserAsync(string filterEmail) => await FilterAsync<User>(filterApiUri, filterEmail, parmName: nameof(filterEmail));

        public async Task<User> GetUserAsync(string email) => await GetAsync<User>(apiUri, email, parmName: nameof(email));
        public async Task CreateUserAsync(CreateUserRequest user) => await PostAsync(apiUri, user);
        public async Task UpdateUserAsync(UserRequest user) => await PutAsync(apiUri, user);
        public async Task DeleteUserAsync(string email) => await DeleteAsync(apiUri, email, parmName: nameof(email));

    }
}
