using FoxIDs.Infrastructure;
using FoxIDs.Models.Config;
using FoxIDs.Models.Sequences;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SequenceLogic : LogicBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly IDistributedCache distributedCache;
        private readonly LocalizationLogic localizationLogic;

        public SequenceLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IDataProtectionProvider dataProtectionProvider, IDistributedCache distributedCache, LocalizationLogic localizationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataProtectionProvider = dataProtectionProvider;
            this.distributedCache = distributedCache;
            this.localizationLogic = localizationLogic;
        }

        public async Task StartSequenceAsync()
        {
            try
            {
                var sequence = new Sequence
                {
                    Id = RandomGenerator.Generate(12),
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = await CreateSequenceStringAsync(sequence);

                logger.ScopeTrace($"Sequence started, id '{sequence.Id}'.");
                logger.SetScopeProperty("sequenceId", sequence.Id);
            }
            catch (Exception ex)
            {
                throw new SequenceException("Unable to start sequence.", ex);
            }
        }

        public async Task SetCultureAsync(IEnumerable<string> names)
        {
            var culture = await localizationLogic.GetSupportedCultureAsync(names);
            if(!culture.IsNullOrEmpty())
            {
                var sequence = HttpContext.GetSequence();
                sequence.Culture = culture;
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = await CreateSequenceStringAsync(sequence);

                logger.ScopeTrace($"Sequence culture added, id '{sequence.Id}', culture '{culture}'.");
            }
        }

        public Task<Sequence> TryReadSequenceAsync(string sequenceString)
        {
            if (!sequenceString.IsNullOrWhiteSpace())
            {
                try
                {
                    var sequence = CreateProtector().Unprotect(sequenceString).ToObject<Sequence>();
                    return Task.FromResult(sequence);
                }
                catch
                { }
            }
            return Task.FromResult<Sequence>(null);
        }

        public async Task ValidateSequenceAsync(string sequenceString)
        {
            if (sequenceString.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sequenceString));

            try
            {
                var sequence = await Task.FromResult(CreateProtector().Unprotect(sequenceString).ToObject<Sequence>());
                CheckTimeout(sequence);
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = sequenceString;

                logger.ScopeTrace($"Sequence is validated, id '{sequence.Id}'.");
                logger.SetScopeProperty("sequenceId", sequence.Id);
            }
            catch (SequenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SequenceException("Sequence is invalid.", ex);
            }
        }

        public async Task SaveSequenceDataAsync<T>(T data) where T : ISequenceData
        {
            await data.ValidateObjectAsync();

            var sequence = HttpContext.GetSequence();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(sequence.CreateTime).AddSeconds(HttpContext.GetRouteBinding().SequenceLifetime)
            };
            await distributedCache.SetStringAsync(DataKey(typeof(T), sequence), data.ToJson(), options);
        }

        public async Task<T> GetSequenceDataAsync<T>(bool remove = true) where T : ISequenceData
        {
            var sequence = HttpContext.GetSequence();
            var key = DataKey(typeof(T), sequence);
            var data = await distributedCache.GetStringAsync(key);
            if(data == null)
            {
                throw new SequenceException($"Cache do not contain the sequence object with sequence id '{sequence.Id}'.");
            }

            if(remove)
            {
                await distributedCache.RemoveAsync(key);
            }

            return data.ToObject<T>();
        }

        public async Task RemoveSequenceDataAsync<T>() where T : ISequenceData
        {
            var sequence = HttpContext.GetSequence();
            var key = DataKey(typeof(T), sequence);
            await distributedCache.RemoveAsync(key);
        }

        private Task<string> CreateSequenceStringAsync(Sequence sequence)
        {
            return Task.FromResult(CreateProtector().Protect(sequence.ToJson()));
        }

        private string DataKey(Type type, Sequence sequence)
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return $"{routeBinding.TenantName}.{routeBinding.TrackName}.{type.Name.ToLower()}.{sequence.Id}.{sequence.CreateTime}";
        }

        private IDataProtector CreateProtector()
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return dataProtectionProvider.CreateProtector(new[] { routeBinding.TenantName, routeBinding.TrackName, typeof(SequenceLogic).Name });
        }

        private void CheckTimeout(Sequence sequence) 
        {
            var now = DateTimeOffset.UtcNow;
            var createTime = DateTimeOffset.FromUnixTimeSeconds(sequence.CreateTime);

            if (createTime.AddSeconds(HttpContext.GetRouteBinding().SequenceLifetime) < now)
            {
                throw new SequenceTimeoutException($"Sequence timeout, id '{sequence.Id}'.");
            }
        }
    }
}
