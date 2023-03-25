# Interconnect two FoxIDs tracks with a track link

FoxIDs tracks in the same tenant can be connected with track links. A track link acts mostly like OpenID Connect but it is simpler to configure and the steps it goes through is faster.  
Therefor a login sequence that jumps between tracks will execute faster using a track link competed with using OpenID Connect. But an [OpenID connect connection](howto-oidc-foxids.md) is required if you need to jump between tracks located in different tenants.

Track links support login, RP-initiated logout and front-channel logout. Furthermore, it is possible to configure [claim and claim transforms](claim.md), logout session and home realm discovery (HRD) like all other connecting up-parties and down-parties.

## Configure integration

The following describes how to connect two tracks called `track_x` and `track_y` where `track_y` become an up-party on `track_x`.

**1 - Start in the `track_x` track by creating a track link in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Select the Parties tab and then the Up-parties
2. Click Create up-party and then Track link
3. Add the name e.g., `track_y-connection` 
4. Add the `track_y` track name
5. Add the down-party name in the `track_y` track e.g., `track_x-connection` 
6. Click Create

![Create track link up-party](images/howto-tracklink-foxids-up-party.png)

**2 - Then go to the `track_y` track and create a track link in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Select the Parties tab and then the Down-parties
2. Click Create down-party and then Track link
3. Add the name e.g., `track_x-connection` 
4. Add the `track_x` track name
5. Add the up-party name in the `track_x` track e.g., `track_y-connection` 
6. Select which up-parties in the `track_y` track the user is allowed to use for authentication
6. Click Create

![Create track link down-party](images/howto-tracklink-foxids-down-party.png)

That's it, you are done. 

> Your new up-party `track_y-connection` can now be selected as an allowed up-party in the down-parties in you `track_x` track.  
> The down-parties in you `track_x` track can read the claims from your `track_y-connection` up-party. 