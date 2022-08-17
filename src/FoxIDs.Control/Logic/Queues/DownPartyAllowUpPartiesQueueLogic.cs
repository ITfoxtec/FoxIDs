using FoxIDs.Infrastructure;
using FoxIDs.Infrastructure.Queue;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Queue;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class DownPartyAllowUpPartiesQueueLogic : LogicBase, IQueueProcessingService
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IConnectionMultiplexer redisConnectionMultiplexer;
        private readonly ITenantRepository tenantRepository;

        public DownPartyAllowUpPartiesQueueLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, IConnectionMultiplexer redisConnectionMultiplexer, ITenantRepository tenantRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.redisConnectionMultiplexer = redisConnectionMultiplexer;
            this.tenantRepository = tenantRepository;
        }

        public async Task UpdateUpParty(UpParty upParty)
        {
            await AddToQueue(upParty, false);
        }

        public async Task DeleteUpParty(UpParty upParty)
        {
            await AddToQueue(upParty, true);
        }

        private async Task AddToQueue(UpParty upParty, bool remove)
        {
            var message = new UpPartyHrdQueueMessage
            {
                Name = upParty.Name,
                HrdDisplayName = upParty.HrdDisplayName,
                HrdDomains = upParty.HrdDomains,
                HrdLogoUrl = upParty.HrdLogoUrl,
                Remove = remove
            };
            await message.ValidateObjectAsync();

            var routeBinding = RouteBinding;
            var envalope = new QueueEnvelope
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                LogicClassType = GetType(),
                Info = remove ? $"Remove up-party '{upParty.Name}' from down-parties allow up-party list" : $"Update up-party '{upParty.Name}' in down-parties allow up-party list",
                Message = message.ToJson(),
            };
            await envalope.ValidateObjectAsync();

            var db = redisConnectionMultiplexer.GetDatabase();
            await db.ListLeftPushAsync(BackgroundQueueService.QueueKey, envalope.ToJson());

            var sub = redisConnectionMultiplexer.GetSubscriber();
            await sub.PublishAsync(BackgroundQueueService.QueueEventKey, string.Empty);
        }

        public async Task DoWorkAsync(string message, CancellationToken stoppingToken)
        {
            var upPartyHrdQueueMessage = message.ToObject<UpPartyHrdQueueMessage>();
            await upPartyHrdQueueMessage.ValidateObjectAsync();


            //await tenantRepository.GetListAsync<DownParty>()



        }

    }
}
