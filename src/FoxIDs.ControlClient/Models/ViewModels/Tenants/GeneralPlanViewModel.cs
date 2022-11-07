using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralPlanViewModel : Plan
    {
        public GeneralPlanViewModel()
        { }

        public GeneralPlanViewModel(Plan plan)
        {
            Name = plan.Name;
        }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<Plan> Form { get; set; }
    }
}
