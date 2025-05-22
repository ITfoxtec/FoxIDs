using FoxIDs.Models;
using FoxIDs.Repository;
using Microsoft.Extensions.DependencyInjection;
using Wololo.PgKeyValueDB;

namespace FoxIDs.PGMigrator;

public class Migrator(
    CosmosDbTenantDataRepository cosmosDbTenantDataRepository,
    [FromKeyedServices(Constants.Models.DataType.Tenant)] PgKeyValueDB tenantDb
    )
{
    public async Task RunAsync()
    {
        var tenants = await cosmosDbTenantDataRepository.GetManyAsync<Tenant>();
        foreach (var tenant in tenants.items)
        {
            await tenantDb.CreateAsync(tenant.Id, tenant, tenant.PartitionId);
            var idKey = new Track.IdKey { TenantName = tenant.Name, TrackName = "" };
            var tracks = await cosmosDbTenantDataRepository.GetManyAsync<Track>(idKey, t => t.DataType.Equals(Constants.Models.DataType.Track));
            foreach (var track in tracks.items)
            {
                await tenantDb.CreateAsync(track.Id, track, track.PartitionId);
                idKey = new Track.IdKey { TenantName = tenant.Name, TrackName = track.Name };
                var users = await cosmosDbTenantDataRepository.GetManyAsync<User>(idKey, t => t.DataType.Equals(Constants.Models.DataType.User));
                foreach (var user in users.items)
                {
                    await tenantDb.CreateAsync(user.Id, user, user.PartitionId);
                }
                var downparties = await cosmosDbTenantDataRepository.GetManyAsync<DownParty>(idKey, t => t.DataType.Equals(Constants.Models.DataType.DownParty));
                foreach (var downparty in downparties.items)
                {
                    if (downparty.Type == PartyTypes.OAuth2) {
                        var oAuthDownParty = await cosmosDbTenantDataRepository.GetAsync<OAuthDownParty>(downparty.Id);
                        await tenantDb.CreateAsync(oAuthDownParty.Id, oAuthDownParty, oAuthDownParty.PartitionId);
                    }
                    else if (downparty.Type == PartyTypes.Oidc) {
                        var oidcDownParty = await cosmosDbTenantDataRepository.GetAsync<OidcDownParty>(downparty.Id);
                        await tenantDb.CreateAsync(oidcDownParty.Id, oidcDownParty, oidcDownParty.PartitionId);
                    }
                }
                var upparties = await cosmosDbTenantDataRepository.GetManyAsync<UpParty>(idKey, t => t.DataType.Equals(Constants.Models.DataType.UpParty));
                foreach (var upparty in upparties.items)
                {
                    if (upparty.Type == PartyTypes.Saml2) {
                        var samlUpParty = await cosmosDbTenantDataRepository.GetAsync<SamlUpParty>(upparty.Id);
                        await tenantDb.CreateAsync(samlUpParty.Id, samlUpParty, samlUpParty.PartitionId);
                    }
                    else if (upparty.Type == PartyTypes.Oidc) {
                        var oidcUpParty = await cosmosDbTenantDataRepository.GetAsync<OidcUpParty>(upparty.Id);
                        await tenantDb.CreateAsync(oidcUpParty.Id, oidcUpParty, oidcUpParty.PartitionId);
                    }
                    else if (upparty.Type == PartyTypes.Login) {
                        var oidcUpParty = await cosmosDbTenantDataRepository.GetAsync<LoginUpParty>(upparty.Id);
                        await tenantDb.CreateAsync(oidcUpParty.Id, oidcUpParty, oidcUpParty.PartitionId);
                    }
                }
            }
        }
    }
}