using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UserService : BaseService
    {
        private const string apiUri = "api/{tenant}/{track}/!user";
        private const string listApiUri = "api/{tenant}/{track}/!users";
        private const string myUserApiUri = "api/{tenant}/master/!myuser";
        private const string userControlProfileApiUri = "api/{tenant}/master/!usercontrolprofile";

        public UserService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<User>> GetUsersAsync(string filterEmail, string filterPhone, string filterUsername, string filterUserId, string paginationToken = null) => await GetListAsync<User>(listApiUri, filterEmail, filterPhone, filterUsername, filterUserId, parmName1: nameof(filterEmail), parmName2: nameof(filterPhone), parmName3: nameof(filterUsername), parmName4: nameof(filterUserId), paginationToken: paginationToken);

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
