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
- `https://--foxids-control-domain--/api/swagger/v2/swagger.json`

## Health check query parameters

The `/health` endpoint accepts optional query parameters that allow you to verify specific dependencies individually. When no parameters are supplied, the endpoint returns `200 OK` to confirm the site is running without validating external services.  
Use one or more of the following parameters (case-insensitive):

| Parameter                      | Description                                      | Works for                                  |
|--------------------------------|--------------------------------------------------|--------------------------------------------|
| `?db`                          | Verifies data storage by ensuring the master tenant document exists. | All supported databases. |
| `?log`                         | Runs a logging check. OpenSearch validates rollover aliases; Application Insights sends a trace. | When logging is configured for OpenSearch or Application Insights. |
| `?cache`                       | Executes a Redis PING command.                   | When Redis cache is configured.            |
| `?all`                         | Automatically checks every component that is enabled in configuration. |                      |

An invalid component name returns `400 Bad Request` with a JSON response describing the issue. If any requested component is unhealthy, the endpoint returns `503 Service Unavailable` and lists the failing checks.

