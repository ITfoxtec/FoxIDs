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
        private const string setPasswordApiUri = "api/{tenant}/{track}/!usersetpassword";
        private const string myUserApiUri = "api/{tenant}/master/!myuser";
        private const string userControlProfileApiUri = "api/{tenant}/master/!usercontrolprofile";
        private const string failingLoginLockApiUri = "api/{tenant}/{track}/!failingloginlock";
        private const string failingLoginLocksApiUri = "api/{tenant}/{track}/!failingloginlocks";
        private const string refreshTokenGrantApiUri = "api/{tenant}/{track}/!refreshtokengrant";
        private const string refreshTokenGrantsApiUri = "api/{tenant}/{track}/!refreshtokengrants";
        private const string activeSessionApiUri = "api/{tenant}/{track}/!activesession";
        private const string activeSessionsApiUri = "api/{tenant}/{track}/!activesessions";

        public UserService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<User>> GetUsersAsync(string filterEmail, string filterPhone, string filterUsername, string filterUserId, string paginationToken = null) => await GetListAsync<User>(listApiUri, filterEmail, filterPhone, filterUsername, filterUserId, parmName1: nameof(filterEmail), parmName2: nameof(filterPhone), parmName3: nameof(filterUsername), parmName4: nameof(filterUserId), paginationToken: paginationToken);

        public async Task<User> GetUserAsync(string email, string phone, string username) => await GetAsync<User>(apiUri, email, phone, username, parmName1: nameof(email), parmName2: nameof(phone), parmName3: nameof(username));
        public async Task<User> CreateUserAsync(CreateUserRequest user) => await PostResponseAsync<CreateUserRequest, User>(apiUri, user);
        public async Task<User> UpdateUserAsync(UserRequest user) => await PutResponseAsync<UserRequest, User>(apiUri, user);
        public async Task<User> SetUserPasswordAsync(UserSetPasswordRequest request) => await PutResponseAsync<UserSetPasswordRequest, User>(setPasswordApiUri, request);
        public async Task DeleteUserAsync(string email, string phone, string username) => await DeleteAsync(apiUri, email, phone, username, parmName1: nameof(email), parmName2: nameof(phone), parmName3: nameof(username));

        public async Task<MyUser> GetMyUserAsync() => await GetAsync<MyUser>(myUserApiUri);
        public async Task<MyUser> UpdateMyUserAsync(MyUser myUser) => await PutResponseAsync<MyUser, MyUser>(myUserApiUri, myUser);

        public async Task<UserControlProfile> GetUserControlProfileAsync() => await GetAsync<UserControlProfile>(userControlProfileApiUri);
        public async Task<UserControlProfile> UpdateUserControlProfileAsync(UserControlProfile userControlProfile) => await PutResponseAsync<UserControlProfile, UserControlProfile>(userControlProfileApiUri, userControlProfile);
        public async Task DeleteUserControlProfileAsync() => await DeleteAsync(userControlProfileApiUri);

        public async Task<PaginationResponse<FailingLoginLock>> GetFailingLoginLocksAsync(string filterUserIdentifier, FailingLoginTypes? filterFailingLoginType = null, string paginationToken = null) => await GetListAsync<FailingLoginLock>(failingLoginLocksApiUri, filterUserIdentifier, filterFailingLoginType?.ToString(), parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterFailingLoginType), paginationToken: paginationToken);
        public async Task<FailingLoginLock> GetFailingLoginLockAsync(string userIdentifier, FailingLoginTypes failingLoginType) => await GetAsync<FailingLoginLock>(failingLoginLockApiUri, userIdentifier, failingLoginType.ToString(), parmName1: nameof(userIdentifier), parmName2: nameof(failingLoginType));
        public async Task DeleteFailingLoginLockAsync(string userIdentifier, FailingLoginTypes failingLoginType) => await DeleteAsync(failingLoginLockApiUri, userIdentifier, failingLoginType.ToString(), parmName1: nameof(userIdentifier), parmName2: nameof(failingLoginType));

        public async Task<PaginationResponse<RefreshTokenGrant>> GetRefreshTokenGrantsAsync(string filterUserIdentifier, string filterSub, string filterClientId, string filterUpPartyName, string filterSessionId, string paginationToken = null) => await GetListAsync<RefreshTokenGrant>(refreshTokenGrantsApiUri, filterUserIdentifier, filterSub, filterClientId, filterUpPartyName, filterSessionId, parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterSub), parmName3: nameof(filterClientId), parmName4: nameof(filterUpPartyName), parmName5: nameof(filterSessionId), paginationToken: paginationToken);
        public async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken) => await GetAsync<RefreshTokenGrant>(refreshTokenGrantApiUri, refreshToken, parmName1: nameof(refreshToken));
        public async Task DeleteRefreshTokenGrantsAsync(string userIdentifier = null, string sub = null, string clientId = null, string upPartyName = null, string sessionId = null) => await DeleteAsync(refreshTokenGrantsApiUri, userIdentifier, sub, clientId, upPartyName, sessionId, parmName1: nameof(userIdentifier), parmName2: nameof(sub), parmName3: nameof(clientId), parmName4: nameof(upPartyName), parmName5: nameof(sessionId));

        public async Task<ActiveSession> GetActiveSessionAsync(string sessionId) => await GetAsync<ActiveSession>(activeSessionApiUri, sessionId, parmName1: nameof(sessionId));
        public async Task<PaginationResponse<ActiveSession>> GetActiveSessionsAsync(string filterUserIdentifier, string filterSub, string filterDownPartyName, string filterUpPartyName, string filterSessionId, string paginationToken = null) => await GetListAsync<ActiveSession>(activeSessionsApiUri, filterUserIdentifier, filterSub, filterDownPartyName, filterUpPartyName, filterSessionId, parmName1: nameof(filterUserIdentifier), parmName2: nameof(filterSub), parmName3: nameof(filterDownPartyName), parmName4: nameof(filterUpPartyName), parmName5: nameof(filterSessionId), paginationToken: paginationToken);
        public async Task DeleteActiveSessionAsync(string sessionId) => await DeleteAsync(activeSessionApiUri, sessionId, parmName1: nameof(sessionId));
        public async Task DeleteActiveSessionsAsync(string userIdentifier = null, string sub = null, string downPartyName = null, string upPartyName = null, string sessionId = null) => await DeleteAsync(activeSessionsApiUri, userIdentifier, sub, downPartyName, upPartyName, sessionId, parmName1: nameof(userIdentifier), parmName2: nameof(sub), parmName3: nameof(downPartyName), parmName4: nameof(upPartyName), parmName5: nameof(sessionId));
    }
}
