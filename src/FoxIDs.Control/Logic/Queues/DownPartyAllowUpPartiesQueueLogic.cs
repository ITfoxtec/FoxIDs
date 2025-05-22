using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models.Queues;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Queues
{
    public class DownPartyAllowUpPartiesQueueLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly BackgroundQueue backgroundQueue;
        private readonly ITenantDataRepository tenantDataRepository;
        private readonly DownPartyCacheLogic downPartyCacheLogic;

        public DownPartyAllowUpPartiesQueueLogic(TelemetryScopedLogger logger, BackgroundQueue backgroundQueue, ITenantDataRepository tenantDataRepository, DownPartyCacheLogic downPartyCacheLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.backgroundQueue = backgroundQueue;
            this.tenantDataRepository = tenantDataRepository;
            this.downPartyCacheLogic = downPartyCacheLogic;
        }

        public async Task UpdateUpParty(UpPartyWithProfile<UpPartyProfile> oldUpParty, UpParty newUpParty, IEnumerable<Api.IProfile> newProfiles)
        {
            var messages = new List<UpPartyHrdQueueMessage>();

            var action = UpPartyHrdHasChanged(oldUpParty, newUpParty);
            if (action != UpPartyHrdQueueMessageActions.None)
            {
                await AddUpPartyToQueue(messages, oldUpParty, newUpParty, action);
            }

            if (oldUpParty != null && oldUpParty.Profiles?.Count > 0)
            {
                foreach (var profile in oldUpParty.Profiles)
                {
                    (var actionProfile, var newProfile) = UpPartyProfileHrdHasChanged(profile, newProfiles);
                    if (actionProfile == UpPartyHrdQueueMessageActions.RemoveProfile)
                    {
                        await AddToQueue(messages, new UpPartyHrdQueueMessage { ProfileName = profile.Name, MessageAction = actionProfile });
                    }
                    else if(actionProfile != UpPartyHrdQueueMessageActions.None)
                    {
                        await AddUpPartyProfileToQueue(messages, newProfile, actionProfile);
                    }
                }
            }

            StartWork(oldUpParty.Name, messages);
        }

        private UpPartyHrdQueueMessageActions UpPartyHrdHasChanged(UpParty oldUpParty, UpParty newUpParty)
        {
            var update = false;
            var nameChange = false;

            if (oldUpParty != null)
            {
                if (oldUpParty.Name != newUpParty.Name)
                {
                    update = true;
                    nameChange = true;
                }
                if (oldUpParty.DisplayName != newUpParty.DisplayName)
                {
                    update = true;
                }

                var oldHrdIssuers = oldUpParty.Issuers != null ? string.Join(',', oldUpParty.Issuers) : string.Empty;
                var newHrdIssuers = newUpParty.Issuers != null ? string.Join(',', newUpParty.Issuers) : string.Empty;
                if (oldHrdIssuers != newHrdIssuers)
                {
                    update = true;
                }

                if (oldUpParty.SpIssuer != newUpParty.SpIssuer)
                {
                    update = true;
                }

                var oldHrdIPAddressesAndRanges = oldUpParty.HrdIPAddressesAndRanges != null ? string.Join(',', oldUpParty.HrdIPAddressesAndRanges) : string.Empty;
                var newHrdIPAddressesAndRanges = newUpParty.HrdIPAddressesAndRanges != null ? string.Join(',', newUpParty.HrdIPAddressesAndRanges) : string.Empty;
                if (oldHrdIPAddressesAndRanges != newHrdIPAddressesAndRanges)
                {
                    update = true;
                }

                var oldHrdDomains = oldUpParty.HrdDomains != null ? string.Join(',', oldUpParty.HrdDomains) : string.Empty;
                var newHrdDomains = newUpParty.HrdDomains != null ? string.Join(',', newUpParty.HrdDomains) : string.Empty;
                if (oldHrdDomains != newHrdDomains)
                {
                    update = true;
                }

                var oldHrdRegularExpressions = oldUpParty.HrdRegularExpressions != null ? string.Join(',', oldUpParty.HrdRegularExpressions) : string.Empty;
                var newHrdRegularExpressions = newUpParty.HrdRegularExpressions != null ? string.Join(',', newUpParty.HrdRegularExpressions) : string.Empty;
                if (oldHrdRegularExpressions != newHrdRegularExpressions)
                {
                    update = true;
                }

                if (oldUpParty.HrdAlwaysShowButton != newUpParty.HrdAlwaysShowButton)
                {
                    update = true;
                }

                if (oldUpParty.HrdDisplayName != newUpParty.HrdDisplayName)
                {
                    update = true;
                }

                if (oldUpParty.HrdLogoUrl != newUpParty.HrdLogoUrl)
                {
                    update = true;
                }
            }

            return update ? (nameChange ? UpPartyHrdQueueMessageActions.ChangeName : UpPartyHrdQueueMessageActions.Update) : UpPartyHrdQueueMessageActions.None;
        }

        private (UpPartyHrdQueueMessageActions, Api.IProfile) UpPartyProfileHrdHasChanged(UpPartyProfile profile, IEnumerable<Api.IProfile> newProfiles)
        {
            var newProfile = newProfiles?.Where(p => p.Name == profile.Name).FirstOrDefault();
            if (newProfile != null)
            {
                if(!newProfile.NewName.IsNullOrWhiteSpace())
                {
                    return (UpPartyHrdQueueMessageActions.ChangeProfileName, newProfile);
                }

                if (profile.DisplayName != newProfile.DisplayName)
                {
                    return (UpPartyHrdQueueMessageActions.UpdateProfile, newProfile);
                }
            }
            else
            {
                return (UpPartyHrdQueueMessageActions.RemoveProfile, null);
            }

            return (UpPartyHrdQueueMessageActions.None, null);
        }       

        public async Task DeleteUpParty(string upPartyName)
        {
            var messages = new List<UpPartyHrdQueueMessage>();
            await AddToQueue(messages, new UpPartyHrdQueueMessage { MessageAction = UpPartyHrdQueueMessageActions.Remove });
            StartWork(upPartyName, messages);
        }

        private async Task AddToQueue(List<UpPartyHrdQueueMessage> messages, UpPartyHrdQueueMessage message)
        {
            await message.ValidateObjectAsync();
            messages.Add(message);
        }

        private async Task AddUpPartyToQueue(List<UpPartyHrdQueueMessage> messages, UpPartyWithProfile<UpPartyProfile> oldUpParty, UpParty newUpParty, UpPartyHrdQueueMessageActions messageAction)
        {
            var message = new UpPartyHrdQueueMessage
            {
                DisplayName = newUpParty.DisplayName,                
                Issuers = newUpParty.Issuers,
                SpIssuer = newUpParty.SpIssuer,
                HrdDisplayName = newUpParty.HrdDisplayName,
                HrdIPAddressesAndRanges = newUpParty.HrdIPAddressesAndRanges,
                HrdDomains = newUpParty.HrdDomains,
                HrdRegularExpressions = newUpParty.HrdRegularExpressions,
                HrdAlwaysShowButton = newUpParty.HrdAlwaysShowButton,
                HrdLogoUrl = newUpParty.HrdLogoUrl,
                DisableUserAuthenticationTrust = newUpParty.DisableUserAuthenticationTrust,
                DisableTokenExchangeTrust = newUpParty.DisableTokenExchangeTrust,
                MessageAction = messageAction
            };

            if(messageAction == UpPartyHrdQueueMessageActions.ChangeName)
            {
                message.NewName = newUpParty.Name;
            }

            await AddToQueue(messages, message);
        }

        private async Task AddUpPartyProfileToQueue(List<UpPartyHrdQueueMessage> messages, Api.IProfile newProfile, UpPartyHrdQueueMessageActions messageAction)
        {
            var message = new UpPartyHrdQueueMessage
            {
                ProfileName = newProfile.Name,
                ProfileDisplayName = newProfile.DisplayName,
                MessageAction = messageAction
            };

            if (messageAction == UpPartyHrdQueueMessageActions.ChangeProfileName)
            {
                message.NewProfileName = newProfile.NewName;
            }

            await AddToQueue(messages, message);
        }

        private void StartWork(string upPartyName, IEnumerable<UpPartyHrdQueueMessage> messages)
        {
            var routeBinding = RouteBinding;
            backgroundQueue.QueueBackgroundWorkItem(async (stoppingToken) =>
            {
                try
                {
                    var info = $"Update applications allowed authentication for methodsAuthentication method name '{upPartyName}'.";
                    logger.Event($"Start to process '{info}'.");
                    await DoWorkAsync(routeBinding.TenantName, routeBinding.TrackName, upPartyName, messages, stoppingToken);
                    logger.Event($"Done processing '{info}'.");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Background queue error.");
                }
            });
        }

        public async Task DoWorkAsync(string tenantName, string trackName, string upPartyName, IEnumerable<UpPartyHrdQueueMessage> messages, CancellationToken stoppingToken)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            string paginationToken = null;
            while (!stoppingToken.IsCancellationRequested) 
            {
                (var downParties, paginationToken) = await tenantDataRepository.GetManyAsync<DownParty>(idKey, whereQuery: p => p.DataType == Constants.Models.DataType.DownParty && p.AllowUpParties.Where(up => up.Name == upPartyName).Any(), pageSize: 100, paginationToken: paginationToken, scopedLogger: logger);
                foreach (var downParty in downParties)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await UpdateDownPartyAsync(tenantName, trackName, upPartyName, downParty, messages);
                }
                
                if (paginationToken == null)
                {
                    break;
                } 
            }
        }

        private async Task UpdateDownPartyAsync(string tenantName, string trackName, string upPartyName, DownParty downParty, IEnumerable<UpPartyHrdQueueMessage> messages)
        {
            switch (downParty.Type)
            {
                case PartyTypes.Oidc:
                    await UpdateDownPartyAsync<OidcDownParty>(tenantName, trackName, upPartyName, downParty, messages);
                    break;
                case PartyTypes.Saml2:
                    await UpdateDownPartyAsync<SamlDownParty>(tenantName, trackName, upPartyName, downParty, messages);
                    break;
                case PartyTypes.OAuth2:
                    await UpdateDownPartyAsync<OAuthDownParty>(tenantName, trackName, upPartyName, downParty, messages);
                    break;
                case PartyTypes.TrackLink:
                    await UpdateDownPartyAsync<TrackLinkDownParty>(tenantName, trackName, upPartyName, downParty, messages);
                    break;
                default:
                    throw new NotSupportedException($"Application registration type {downParty.Type} not supported.");
            }
        }

        private async Task UpdateDownPartyAsync<T>(string tenantName, string trackName, string upPartyName, DownParty downParty, IEnumerable<UpPartyHrdQueueMessage> messages) where T : DownParty
        {
            var idKey = new Party.IdKey
            {
                TenantName = tenantName,
                TrackName = trackName,
                PartyName = downParty.Name
            };
            var downPartyFullObj = await tenantDataRepository.GetAsync<T>(await DownParty.IdFormatAsync(idKey), scopedLogger: logger);

            var removeMessages = messages.Where(m => m.MessageAction == UpPartyHrdQueueMessageActions.Remove || m.MessageAction == UpPartyHrdQueueMessageActions.RemoveProfile);
            foreach(var removeMessage in removeMessages)
            {
                if (removeMessage.MessageAction == UpPartyHrdQueueMessageActions.Remove)
                {
                    downPartyFullObj.AllowUpParties.RemoveAll(up => up.Name == upPartyName);
                }
                else if (removeMessage.MessageAction == UpPartyHrdQueueMessageActions.RemoveProfile)
                {
                    downPartyFullObj.AllowUpParties.RemoveAll(up => up.Name == upPartyName && up.ProfileName == removeMessage.ProfileName);
                }
            }

            var allowUpParties = downPartyFullObj.AllowUpParties.Where(up => up.Name == upPartyName).ToList();

            messages = messages.Where(m => m.MessageAction != UpPartyHrdQueueMessageActions.Remove && m.MessageAction != UpPartyHrdQueueMessageActions.RemoveProfile);
            foreach (var message in messages)
            {
                if(message.MessageAction == UpPartyHrdQueueMessageActions.ChangeName)
                {
                    foreach (var allowUpParty in allowUpParties)
                    {
                        allowUpParty.Name = message.NewName;
                    }
                }

                if (message.MessageAction == UpPartyHrdQueueMessageActions.Update || message.MessageAction == UpPartyHrdQueueMessageActions.ChangeName)
                {
                    var upParty = allowUpParties.Where(up => up.ProfileName.IsNullOrEmpty()).FirstOrDefault();
                    if (upParty != null)
                    {
                        upParty.DisplayName = message.DisplayName;
                        upParty.Issuers = message.Issuers;
                        upParty.SpIssuer = message.SpIssuer;
                        upParty.HrdIPAddressesAndRanges = message.HrdIPAddressesAndRanges;
                        upParty.HrdDomains = message.HrdDomains;
                        upParty.HrdRegularExpressions = message.HrdRegularExpressions;
                        upParty.HrdAlwaysShowButton = message.HrdAlwaysShowButton;
                        upParty.HrdDisplayName = message.HrdDisplayName;
                        upParty.HrdLogoUrl = message.HrdLogoUrl;
                        upParty.DisableUserAuthenticationTrust = message.DisableUserAuthenticationTrust;
                        upParty.DisableTokenExchangeTrust = message.DisableTokenExchangeTrust;
                    }
                }
                else if (message.MessageAction == UpPartyHrdQueueMessageActions.UpdateProfile || message.MessageAction == UpPartyHrdQueueMessageActions.ChangeProfileName)
                {
                    var upPartyByProfile = allowUpParties.Where(up => up.ProfileName == message.ProfileName).FirstOrDefault();
                    if (upPartyByProfile != null)
                    {
                        upPartyByProfile.ProfileDisplayName = message.ProfileDisplayName;

                        if (message.MessageAction == UpPartyHrdQueueMessageActions.ChangeProfileName)
                        {
                            upPartyByProfile.ProfileName = message.NewProfileName;
                        }
                    }
                }
            }

            await tenantDataRepository.UpdateAsync(downPartyFullObj, scopedLogger: logger);
            await downPartyCacheLogic.InvalidateDownPartyCacheAsync(idKey);
        }
    }
}
