using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UserService : BaseService
    {
        private const string apiUri = "api/{tenant}/{track}/!user";
        private const string filterApiUri = "api/{tenant}/{track}/!filteruser";
        private const string myUserApiUri = "api/{tenant}/master/!myuser";
        private const string userControlProfileApiUri = "api/{tenant}/master/!usercontrolprofile";

        public UserService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<User>> FilterUserAsync(string filterEmail) => await FilterAsync<User>(filterApiUri, filterEmail, parmName1: nameof(filterEmail));

        public async Task<User> GetUserAsync(string email) => await GetAsync<User>(apiUri, email, parmName: nameof(email));
        public async Task<User> CreateUserAsync(CreateUserRequest user) => await PostResponseAsync<CreateUserRequest, User>(apiUri, user);
        public async Task<User> UpdateUserAsync(UserRequest user) => await PutResponseAsync<UserRequest, User>(apiUri, user);
        public async Task DeleteUserAsync(string email) => await DeleteAsync(apiUri, email, parmName: nameof(email));

        public async Task<MyUser> GetMyUserAsync() => await GetAsync<MyUser>(myUserApiUri);
        public async Task<MyUser> UpdateMyUserAsync(MyUser myUser) => await PutResponseAsync<MyUser, MyUser>(myUserApiUri, myUser);

        public async Task<UserControlProfile> GetUserControlProfileAsync() => await GetAsync<UserControlProfile>(userControlProfileApiUri);
        public async Task<UserControlProfile> UpdateUserControlProfileAsync(UserControlProfile userControlProfile) => await PutResponseAsync<UserControlProfile, UserControlProfile>(userControlProfileApiUri, userControlProfile);
        public async Task DeleteUserControlProfileAsync() => await DeleteAsync(userControlProfileApiUri);
    }
}
