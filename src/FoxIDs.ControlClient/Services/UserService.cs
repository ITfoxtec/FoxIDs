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
        private const string failingLoginLockApiUri = "api/{tenant}/{track}/!failingloginlock";
        private const string failingLoginLocksApiUri = "api/{tenant}/{track}/!failingloginlocks";
        private const string refreshTokenGrantApiUri = "api/{tenant}/{track}/!refreshtokengrant";
        private const string refreshTokenGrantsApiUri = "api/{tenant}/{track}/!refreshtokengrants";

        public UserService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<User>> GetUsersAsync(string filterEmail, string filterPhone, string filterUsername, string filterUserId, string paginationToken = null) => await GetListAsync<User>(listApiUri, filterEmail, filterPhone, filterUsername, filterUserId, parmName1: nameof(filterEmail), parmName2: nameof(filterPhone), parmName3: nameof(filterUsername), parmName4: nameof(filterUserId), paginationToken: paginationToken);

        public async Task<User> GetUserAsync(string email, string phone, string username) => await GetAsync<User>(apiUri, email, phone, username, parmName1: nameof(email), parmName2: nameof(phone), parmName3: nameof(username));
        public async Task<User> CreateUserAsync(CreateUserRequest user) => await PostResponseAsync<CreateUserRequest, User>(apiUri, user);
        public async Task<User> UpdateUserAsync(UserRequest user) => await PutResponseAsync<UserRequest, User>(apiUri, user);
        public async Task DeleteUserAsync(string email, string phone, string username) => await DeleteAsync(apiUri, email, phone, username, parmName1: nameof(email), parmName2: nameof(phone), parmName3: nameof(username));

        public async Task<MyUser> GetMyUserAsync() => await GetAsync<MyUser>(myUserApiUri);
        public async Task<MyUser> UpdateMyUserAsync(MyUser myUser) => await PutResponseAsync<MyUser, MyUser>(myUserApiUri, myUser);

        public async Task<UserControlProfile> GetUserControlProfileAsync() => await GetAsync<UserControlProfile>(userControlProfileApiUri);
        public async Task<UserControlProfile> UpdateUserControlProfileAsync(UserControlProfile userControlProfile) => await PutResponseAsync<UserControlProfile, UserControlProfile>(userControlProfileApiUri, userControlProfile);
        public async Task DeleteUserControlProfileAsync() => await DeleteAsync(userControlProfileApiUri);

        public async Task<PaginationResponse<FailingLoginLock>> GetFailingLoginLocksAsync(string filterUserIdentifier, FailingLoginTypes? filterFailingLoginType = null, string paginationToken = null) => await GetListAsync<FailingLoginLock>(failingLoginLocksApiUri, filterUserIdentifier, filterFailingLoginType?.ToString(), parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterFailingLoginType), paginationToken: paginationToken);
        public async Task<FailingLoginLock> GetFailingLoginLockAsync(string userIdentifier, FailingLoginTypes failingLoginType) => await GetAsync<FailingLoginLock>(failingLoginLockApiUri, userIdentifier, failingLoginType.ToString(), parmName1: nameof(userIdentifier), parmName2: nameof(failingLoginType));
        public async Task DeleteFailingLoginLockAsync(string userIdentifier, FailingLoginTypes failingLoginType) => await DeleteAsync(failingLoginLockApiUri, userIdentifier, failingLoginType.ToString(), parmName1: nameof(userIdentifier), parmName2: nameof(failingLoginType));

        public async Task<PaginationResponse<RefreshTokenGrant>> GetRefreshTokenGrantsAsync(string filterUserIdentifier, string filterClientId, string filterAuthMethod, string paginationToken = null) => await GetListAsync<RefreshTokenGrant>(refreshTokenGrantsApiUri, filterUserIdentifier, filterClientId, filterAuthMethod, parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterClientId), parmName3: nameof(filterAuthMethod), paginationToken: paginationToken);
        public async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken) => await GetAsync<RefreshTokenGrant>(refreshTokenGrantApiUri, refreshToken, parmName1: nameof(refreshToken));
        public async Task DeleteRefreshTokenGrantsAsync(string userIdentifier = null, string clientId = null, string authMethod = null) => await DeleteAsync(refreshTokenGrantsApiUri, userIdentifier, clientId, authMethod, parmName1: nameof(userIdentifier), parmName2: nameof(clientId), parmName3: nameof(authMethod));
    }
}
