using FoxIDs.Infrastructure;
using FoxIDs.Logic.Caches.Providers;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using FoxIDs.Models.Sequences;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class SequenceLogic : LogicSequenceBase
    {
        private readonly FoxIDsSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly IDataProtectionProvider dataProtectionProvider;
        private readonly ICacheProvider cacheProvider;
        private readonly LocalizationLogic localizationLogic;

        public SequenceLogic(FoxIDsSettings settings, TelemetryScopedLogger logger, IDataProtectionProvider dataProtectionProvider, ICacheProvider cacheProvider, LocalizationLogic localizationLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataProtectionProvider = dataProtectionProvider;
            this.cacheProvider = cacheProvider;
            this.localizationLogic = localizationLogic;
        }

        public async Task StartSequenceAsync(bool setStart)
        {
            try
            {
                (var sequenceString, var sequence) = await StartSeparateSequenceAsync();

                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = sequenceString;
                if (setStart)
                {
                    HttpContext.Items[Constants.Sequence.Start] = true;
                }
            }
            catch (SequenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SequenceException("Unable to start sequence.", ex);
            }
        }

        public async Task<(string sequenceString, Sequence sequence)> StartSeparateSequenceAsync(bool? accountAction = null, Sequence currentSequence = null)
        {
            try
            {
                var sequence = new Sequence
                {
                    Id = RandomGenerator.Generate(12),
                    CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    AccountAction = accountAction
                };

                if (currentSequence?.Culture?.IsNullOrEmpty() == false)
                {
                    sequence.Culture = currentSequence.Culture;
                }

                if(currentSequence?.UiUpPartyId.IsNullOrEmpty() == false)
                {
                    sequence.UiUpPartyId = currentSequence.UiUpPartyId;
                }

                var sequenceString = await CreateSequenceStringAsync(sequence);

                logger.ScopeTrace(() => $"Sequence started, id '{sequence.Id}'.", new Dictionary<string, string> { { Constants.Logs.SequenceId, sequence.Id }, { Constants.Logs.AccountAction, accountAction == true ? "true" : "false" } });
                return (sequenceString, sequence);
            }
            catch (Exception ex)
            {
                throw new SequenceException("Unable to start sequence.", ex);
            }
        }

        public async Task SetCultureAsync(IEnumerable<string> names)
        {
            var culture = localizationLogic.GetSupportedCulture(names);
            if(!culture.IsNullOrEmpty())
            {
                var sequence = HttpContext.GetSequence();
                sequence.Culture = culture;
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = await CreateSequenceStringAsync(sequence);

                logger.ScopeTrace(() => $"Sequence culture added, id '{sequence.Id}', culture '{culture}'.");
            }
        }
        public async Task SetDownPartyAsync(string downPartyId, PartyTypes downPartyType)
        {
            if (!downPartyId.IsNullOrEmpty())
            {
                var sequence = HttpContext.GetSequence();
                sequence.DownPartyId = downPartyId;
                sequence.DownPartyType = downPartyType;
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = await CreateSequenceStringAsync(sequence);

                logger.ScopeTrace(() => $"Sequence application registration added, id '{sequence.Id}', downPartyId '{downPartyId}', downPartyType '{downPartyType}'.");
            }
        }
        public async Task SetUiUpPartyIdAsync(string uiUpPartyId)
        {
            if(!uiUpPartyId.IsNullOrEmpty())
            {
                var sequence = HttpContext.GetSequence();
                sequence.UiUpPartyId = uiUpPartyId;
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = await CreateSequenceStringAsync(sequence);

                logger.ScopeTrace(() => $"Sequence UI authentication method added, id '{sequence.Id}', uiUpPartyId '{uiUpPartyId}'.");
            }
        }

        public async Task<string> GetUiUpPartyIdAsync(Sequence sequence = null)
        {
            sequence = sequence ?? Sequence;
            return !sequence.UiUpPartyId.IsNullOrEmpty() ? sequence.UiUpPartyId : await UpParty.IdFormatAsync(RouteBinding, Constants.DefaultLogin.Name);
        }

        public Task<Sequence> TryReadSequenceAsync(string sequenceString)
        {
            if (!sequenceString.IsNullOrWhiteSpace())
            {
                sequenceString.ValidateMaxLength(Constants.Sequence.MaxLength, nameof(sequenceString), nameof(TryReadSequenceAsync));

                try
                {
                    var sequence = Unprotect(sequenceString);
                    if (sequence != null)
                    {
                        logger.SetScopeProperty(Constants.Logs.SequenceId, sequence.Id);
                    }
                    return Task.FromResult(sequence);
                }
                catch
                { }
            }
            return Task.FromResult<Sequence>(null);
        }

        public async Task ValidateAndSetSequenceAsync(string sequenceString, bool setValid = false)
        {
            if (sequenceString.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sequenceString));
            sequenceString.ValidateMaxLength(Constants.Sequence.MaxLength, nameof(sequenceString), nameof(ValidateAndSetSequenceAsync));

            try
            {
                var sequence = await Task.FromResult(Unprotect(sequenceString));
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = sequenceString;
                CheckTimeout(sequence);
                if (setValid)
                {
                    HttpContext.Items[Constants.Sequence.Valid] = true;
                }

                logger.ScopeTrace(() => $"Sequence is validated, id '{sequence.Id}'.", new Dictionary<string, string> { { Constants.Logs.SequenceId, sequence.Id }, { Constants.Logs.AccountAction, sequence.AccountAction == true ? "true" : "false" } });
            }
            catch (SequenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SequenceException("Invalid sequence.", ex);
            }
        }

        public async Task<Sequence> ValidateSequenceAsync(string sequenceString, string trackName = null)
        {
            if (sequenceString.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(sequenceString));
            sequenceString.ValidateMaxLength(Constants.Sequence.MaxLength, nameof(sequenceString), nameof(ValidateAndSetSequenceAsync));

            try
            {
                var sequence = await Task.FromResult(Unprotect(sequenceString, trackName));
                CheckTimeout(sequence);

                logger.ScopeTrace(() => $"Sequence is validated, id '{sequence.Id}'.");
                return sequence;
            }
            catch (SequenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SequenceException("Invalid sequence.", ex);
            }
        }

        public async Task<T> SaveSequenceDataAsync<T>(T data, Sequence sequence = null, string trackName = null, bool setKeyValidUntil = false, string partyName = null) where T : ISequenceData
        {
            if (setKeyValidUntil && data is ISequenceKey keyData)
            {
                keyData.KeyValidUntil = DateTimeOffset.UtcNow.AddSeconds(settings.KeySequenceLifetime).ToUnixTimeSeconds();
            }

            await data.ValidateObjectAsync();

            sequence = sequence ?? HttpContext.GetSequence();
            var lifetime = sequence.AccountAction == true ? settings.AccountActionSequenceLifetime : HttpContext.GetRouteBinding().SequenceLifetime;
            if (data is IDownSequenceData)
                lifetime += settings.SequenceGracePeriod;
            await cacheProvider.SetAsync(DataKey(typeof(T), sequence, trackName: trackName, partyName: partyName), data.ToJson(), lifetime);
            return data;
        }

        public async Task<T> GetSequenceDataAsync<T>(bool remove = true, bool allowNull = false, Sequence sequence = null, string trackName = null, string partyName = null) where T : ISequenceData
        {
            sequence = sequence ?? HttpContext.GetSequence(allowNull);
            if(allowNull && sequence == null)
            {
                return default;
            }
            var key = DataKey(typeof(T), sequence, trackName: trackName, partyName: partyName);
            var data = await cacheProvider.GetAsync(key);
            if(data == null)
            {
                if(allowNull)
                {
                    return default;
                }
                else
                {
                    throw new SequenceBrowserBackException($"Cache do not contain the sequence object with sequence id '{sequence.Id}'.");
                }
            }

            var sequenceData = data.ToObject<T>();
            if (settings.DeleteUsedSequences && remove)
            {
                await cacheProvider.DeleteAsync(key);
            }
            return sequenceData;
        }

        public async Task<T> ValidateKeySequenceDataAsync<T>(Sequence sequence, string trackName, bool remove = true, string partyName = null) where T : ISequenceKey
        {
            var keySequenceData = await GetSequenceDataAsync<T>(remove: remove, sequence: sequence, trackName: trackName, partyName: partyName);

            if (keySequenceData.KeyUsed)
            {
                throw new SequenceException($"Key sequence is used, id '{sequence.Id}'.");
            }
            if (!(keySequenceData.KeyValidUntil > 0) || DateTimeOffset.FromUnixTimeSeconds(keySequenceData.KeyValidUntil) < DateTimeOffset.UtcNow)
            {
                throw new SequenceTimeoutException($"Key sequence timeout, id '{sequence.Id}'.");
            }

            if (!remove)
            {
                keySequenceData.KeyUsed = true;
                await SaveSequenceDataAsync(keySequenceData, sequence: sequence, trackName: trackName);
            }
            return keySequenceData;
        }

        public async Task RemoveSequenceDataAsync<T>(string partyName = null) where T : ISequenceData
        {
            if (settings.DeleteUsedSequences)
            {
                var sequence = HttpContext.GetSequence();
                var key = DataKey(typeof(T), sequence, partyName: partyName);
                await cacheProvider.DeleteAsync(key);
            }
        }

        private Task<string> CreateSequenceStringAsync(Sequence sequence)
        {
            return Task.FromResult(Protect(sequence));
        }

        private string DataKey(Type type, Sequence sequence, string trackName = null, string partyName = null)
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return $"{routeBinding.TenantName}.{trackName ?? routeBinding.TrackName}.seq.{type.Name.ToLower()}{(partyName.IsNullOrEmpty() ? string.Empty : $".{partyName}")}.{sequence.Id}.{sequence.CreateTime}";
        }

        private IDataProtector CreateProtector(string trackName = null)
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return dataProtectionProvider.CreateProtector([routeBinding.TenantName, trackName ?? routeBinding.TrackName, typeof(SequenceLogic).Name]);
        }

        private void CheckTimeout(Sequence sequence) 
        {
            var now = DateTimeOffset.UtcNow;
            var createTime = DateTimeOffset.FromUnixTimeSeconds(sequence.CreateTime);

            if(sequence.AccountAction == true)
            {
                if (createTime.AddSeconds(settings.AccountActionSequenceLifetime) < now)
                {
                    throw new SequenceTimeoutException($"Sequence timeout, id '{sequence.Id}'.") { AccountAction = sequence.AccountAction, SequenceLifetime = settings.AccountActionSequenceLifetime };
                }
            }
            else
            {
                if (createTime.AddSeconds(HttpContext.GetRouteBinding().SequenceLifetime) < now)
                {
                    throw new SequenceTimeoutException($"Sequence timeout, id '{sequence.Id}'.") { AccountAction = sequence.AccountAction, SequenceLifetime = HttpContext.GetRouteBinding().SequenceLifetime };
                }
            }
        }

        public async Task<string> CreateExternalSequenceIdAsync()
        {
            var sequence = HttpContext.GetSequence();
            var sequenceString = HttpContext.GetSequenceString();

            var externalId = RandomGenerator.Generate(50);
            var lifetime = sequence.AccountAction == true ? settings.AccountActionSequenceLifetime : HttpContext.GetRouteBinding().SequenceLifetime + settings.SequenceGracePeriod;
            await cacheProvider.SetAsync(ExternalDataKey(externalId), sequenceString, lifetime);
            return externalId;
        }

        public async Task ValidateExternalSequenceIdAsync(string externalId)
        {
            if (externalId.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(externalId));

            try
            {
                var key = ExternalDataKey(externalId);
                var sequenceString = await cacheProvider.GetAsync(key);
                if (sequenceString == null)
                {
                    throw new SequenceBrowserBackException($"Cache do not contain the sequence string with external sequence id '{externalId}'.");
                }
                
                if (settings.DeleteUsedSequences)
                {
                    await cacheProvider.DeleteAsync(key);
                }
                    
                var sequence = await Task.FromResult(Unprotect(sequenceString));
                HttpContext.Items[Constants.Sequence.Object] = sequence;
                HttpContext.Items[Constants.Sequence.String] = sequenceString;
                CheckTimeout(sequence);

                logger.ScopeTrace(() => $"Sequence is validated from external id, sequence id '{sequence.Id}'.", new Dictionary<string, string> { { Constants.Logs.SequenceId, sequence.Id }, { Constants.Logs.ExternalSequenceId, externalId }, { Constants.Logs.AccountAction, sequence.AccountAction == true ? "true" : "false" } });
            }
            catch (SequenceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SequenceException($"Sequence is not loaded from external sequence id '{externalId}'.", ex);
            }
        }

        private string ExternalDataKey(string externalId)
        {
            var routeBinding = HttpContext.GetRouteBinding();
            return $"{routeBinding.TenantName}.{routeBinding.TrackName}.seqext.{externalId}";
        }

        private string Protect(Sequence sequence)
        {
            var sequenceString = CreateProtector().Protect(sequence.ToJson());

            var divideIndex = sequenceString.Length < 255 ? sequenceString.Length / 2 : 250;
            divideIndex = NotDivideNextToUnderline(sequenceString, divideIndex);
            return $"{sequenceString.Substring(0, divideIndex)}/{sequenceString.Substring(divideIndex, sequenceString.Length - divideIndex)}";
        }

        private int NotDivideNextToUnderline(string sequenceString, int divideIndex)
        {
            if (sequenceString[divideIndex + 1] == '_')
            {
                divideIndex--;
                return NotDivideNextToUnderline(sequenceString, divideIndex);
            }

            return divideIndex;
        }

        private Sequence Unprotect(string sequenceString, string trackName = null)
        {
            sequenceString = sequenceString.Remove(sequenceString.IndexOf('/'), 1);
            return CreateProtector(trackName).Unprotect(sequenceString).ToObject<Sequence>();
        }
    }
}
