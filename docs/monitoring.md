# Monitoring
You can collect logs and do health checks
 - If deployed in Azure App Service Container the logs are found in Analytics workspace / Application Insights 
 - If deployed using Docker or Kubernetes logs are written to `Stdout`

No matter the deployment model, your Azure Log Analytics workspace / Application Insights instance can be configure as a [log stream](logging.md#log-stream).  
You can then configure a dashboard in Azure where it is possible to monitor e.g. resources, login events and errors. 

A dashboard can show availability as a result of health checks to:
- `https://--foxids-domain--/health`
- `https://--foxids-domain--/master/master/foxids_control_client(*)/.well-known/openid-configuration`
- `https://--foxids-control-domain--/master`
- `https://--foxids-control-domain--/api/health`
- `https://--foxids-control-domain--/api/swagger/v1/swagger.json`

