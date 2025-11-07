using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading;
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

        public async Task<PaginationResponse<User>> GetUsersAsync(string filterEmail, string filterPhone, string filterUsername, string filterUserId, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<User>(listApiUri, filterEmail, filterPhone, filterUsername, filterUserId, parmName1: nameof(filterEmail), parmName2: nameof(filterPhone), parmName3: nameof(filterUsername), parmName4: nameof(filterUserId), paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<User> GetUserAsync(string email, string phone, string username, CancellationToken cancellationToken = default) => await GetAsync<User>(apiUri, email, phone, username, parmName1: nameof(email), parmName2: nameof(phone), parmName3: nameof(username), cancellationToken: cancellationToken);
        public async Task<User> CreateUserAsync(CreateUserRequest user, CancellationToken cancellationToken = default) => await PostResponseAsync<CreateUserRequest, User>(apiUri, user, cancellationToken);
        public async Task<User> UpdateUserAsync(UserRequest user, CancellationToken cancellationToken = default) => await PutResponseAsync<UserRequest, User>(apiUri, user, cancellationToken);
        public async Task DeleteUserAsync(string email, string phone, string username, CancellationToken cancellationToken = default) => await DeleteAsync(apiUri, email, phone, username, parmName1: nameof(email), parmName2: nameof(phone), parmName3: nameof(username), cancellationToken: cancellationToken);

        public async Task<MyUser> GetMyUserAsync(CancellationToken cancellationToken = default) => await GetAsync<MyUser>(myUserApiUri, cancellationToken);
        public async Task<MyUser> UpdateMyUserAsync(MyUser myUser, CancellationToken cancellationToken = default) => await PutResponseAsync<MyUser, MyUser>(myUserApiUri, myUser, cancellationToken);

        public async Task<UserControlProfile> GetUserControlProfileAsync(CancellationToken cancellationToken = default) => await GetAsync<UserControlProfile>(userControlProfileApiUri, cancellationToken);
        public async Task<UserControlProfile> UpdateUserControlProfileAsync(UserControlProfile userControlProfile, CancellationToken cancellationToken = default) => await PutResponseAsync<UserControlProfile, UserControlProfile>(userControlProfileApiUri, userControlProfile, cancellationToken);
        public async Task DeleteUserControlProfileAsync(CancellationToken cancellationToken = default) => await DeleteAsync(userControlProfileApiUri, cancellationToken);

        public async Task<PaginationResponse<FailingLoginLock>> GetFailingLoginLocksAsync(string filterUserIdentifier, FailingLoginTypes? filterFailingLoginType = null, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<FailingLoginLock>(failingLoginLocksApiUri, filterUserIdentifier, filterFailingLoginType?.ToString(), parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterFailingLoginType), paginationToken: paginationToken, cancellationToken: cancellationToken);
        public async Task<FailingLoginLock> GetFailingLoginLockAsync(string userIdentifier, FailingLoginTypes failingLoginType, CancellationToken cancellationToken = default) => await GetAsync<FailingLoginLock>(failingLoginLockApiUri, userIdentifier, failingLoginType.ToString(), parmName1: nameof(userIdentifier), parmName2: nameof(failingLoginType), cancellationToken: cancellationToken);
        public async Task DeleteFailingLoginLockAsync(string userIdentifier, FailingLoginTypes failingLoginType, CancellationToken cancellationToken = default) => await DeleteAsync(failingLoginLockApiUri, userIdentifier, failingLoginType.ToString(), parmName1: nameof(userIdentifier), parmName2: nameof(failingLoginType), cancellationToken: cancellationToken);

        public async Task<PaginationResponse<RefreshTokenGrant>> GetRefreshTokenGrantsAsync(string filterUserIdentifier, string filterClientId, string filterUpPartyName, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<RefreshTokenGrant>(refreshTokenGrantsApiUri, filterUserIdentifier, filterClientId, filterUpPartyName, parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterClientId), parmName3: nameof(filterUpPartyName), paginationToken: paginationToken, cancellationToken: cancellationToken);
        public async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken, CancellationToken cancellationToken = default) => await GetAsync<RefreshTokenGrant>(refreshTokenGrantApiUri, refreshToken, parmName1: nameof(refreshToken), cancellationToken: cancellationToken);
        public async Task DeleteRefreshTokenGrantsAsync(string userIdentifier = null, string clientId = null, string upPartyName = null, CancellationToken cancellationToken = default) => await DeleteAsync(refreshTokenGrantsApiUri, userIdentifier, clientId, upPartyName, parmName1: nameof(userIdentifier), parmName2: nameof(clientId), parmName3: nameof(upPartyName), cancellationToken: cancellationToken);
    }
}
