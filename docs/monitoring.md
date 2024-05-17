# Monitoring
You can collect logs and monitor FoxIDs
 - If deployed in Azure the logs are found in Analytics workspace / Application Insights 
 - If deployed on-premise in Docker or Kubernetes logs are written to `Stdout`

No matter the deployment model Azure Log Analytics workspace / Application Insights can be configure as a [log stream](logging.md#log-stream) to your monitoring system.  
You can then configure a dashboard in Azure where it is possible to monitor e.g. resources, login events and errors. 

A dashboard can show availability as a result of ping test calls to:
- `https://--foxids-domain--/master/master/foxids_control_client(*)/.well-known/openid-configuration`
- `https://--foxids-control-domain--/master`
- `https://--foxids-control-domain--/api/swagger/v1/swagger.json`

