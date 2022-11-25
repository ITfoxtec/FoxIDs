# Monitoring
You can monitor FoxIDs through Log Analytics workspace / Application Insights or configure a [log stream](logging.md#log-stream) to your monitoring system.

You can configure a dashboard in Log Analytics workspace / Application Insights where it is possible to monitor e.g. resources, login events and errors. 

The dashboard can show availability as a result of ping test calls to:
- `https://--foxids-domain--/master/master/foxids_control_client(*)/.well-known/openid-configuration`
- `https://--foxids-control-domain--/master`
- `https://--foxids-control-domain--/api/swagger/v1/swagger.json`

