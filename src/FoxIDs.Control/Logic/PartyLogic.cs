using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using FoxIDs.Repository;
using FoxIDs.Models;
using System;
using FoxIDs.Util;

namespace FoxIDs.Logic
{
    public class PartyLogic : LogicBase
    {
        private readonly ITenantDataRepository tenantDataRepository;

        public PartyLogic(ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<string> GeneratePartyNameAsync(bool isUpParty, int count = 0)
        {
            var name = RandomName.GenerateDefaultName();
            if (count < Constants.Models.DefaultNameMaxAttempts)
            {
                var mParty = await tenantDataRepository.GetAsync<Party>(isUpParty ? await UpParty.IdFormatAsync(RouteBinding, name) : await DownParty.IdFormatAsync(RouteBinding, name), required: false);
                if (mParty != null)
                {
                    count++;
                    return await GeneratePartyNameAsync(isUpParty, count: count);
                }
            }
            return name;
        }

        public async Task DeleteExporedDownParties()
        {
            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await tenantDataRepository.DeleteListAsync<DownParty>(idKey, whereQuery: p => p.DataType.Equals(Constants.Models.DataType.DownParty) && p.IsTest == true && p.TestExpireAt > 0 && p.TestExpireAt < now);
        }
    }
}
