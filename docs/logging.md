# Logging

Foxids default log errors and events including the time and the client IP address. The logs are sent to Application Insights which is part of the Foxids cloud instance.

## Usage

Foxids usage can be searched in [Foxids Control Client and API](control.md).  

- If you look at the usage in a particular track, the usage of that selected track is shown.  
- If you instead look at the usage in the `master` track it will show the usage for the entire tenant, which can be limited to a particular track. 
- Likewise, the `master` tenant `master` track shows usage for the entire cloud installation of Foxids, which can be limited to a particular tenant and track.

This screen dump shows the usage view in a track.

![Search usage logs](images/search-usage-logs.png)


## Logs

Logs can be searched in [Foxids Control Client and API](control.md).

![Search logs](images/search-logs.png)

## Log settings

The log level can be configured per Foxids track:

 - Enable `Log info trace` - to see details about the login and logout sequences
 - Enable `Log claim trace` - to see the claims authentication methods and application registrations receive and pass on
 - Enable `Log message trace` - to see the raw messages received and sent
 - Enable `Log metric trace` - to see response times and throughput

![Log settings](images/configure-log.png)

## Log stream

It can be configured which logs should be logged to an external repository with a log stream.

Add external Application Insights with a log stream and select which logs should be sent.

![Log stream - Application Insights](images/configure-log-stream-appinsight.png)


