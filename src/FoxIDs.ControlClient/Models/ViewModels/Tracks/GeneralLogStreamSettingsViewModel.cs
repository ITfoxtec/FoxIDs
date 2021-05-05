using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralLogStreamSettingsViewModel
    {
        private LogStreamSettings logStreamSettings;


        public GeneralLogStreamSettingsViewModel()
        {
            LogStreamSettings = new LogStreamSettings { Type = LogStreamTypes.ApplicationInsights };
        }

        public GeneralLogStreamSettingsViewModel(LogStreamSettings logStreamSettings)
        {
            LogStreamSettings = logStreamSettings;
        }

        public bool Edit { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<LogStreamSettings> Form { get; set; }

        public LogStreamSettings LogStreamSettings 
        {
            get
            {
                // Copy object
                return logStreamSettings.Map<LogStreamSettings>();
            }

            set
            {
                if (value.Type == LogStreamTypes.ApplicationInsights)
                {
                    logStreamSettings = value;
                }
                else
                {
                    throw new NotSupportedException("Log stream settings type not supported.");
                }
            }
        }      
    }
}
