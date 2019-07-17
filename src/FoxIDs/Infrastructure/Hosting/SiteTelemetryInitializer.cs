using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;

namespace FoxIDs.Infrastructure.Hosting
{
    public class SiteTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if(!string.IsNullOrEmpty(Activity.Current?.ParentId))
            {
                telemetry.Context.GlobalProperties["activityParentId"] = Activity.Current.ParentId;
            }            
        }
    }
}
