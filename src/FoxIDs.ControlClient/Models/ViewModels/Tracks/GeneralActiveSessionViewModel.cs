using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralActiveSessionViewModel : ActiveSessionViewModel
    {
        public GeneralActiveSessionViewModel()
        { }

        public GeneralActiveSessionViewModel(ActiveSession session)
        {
            SessionId = session.SessionId;
            UpPartyLinks = session.UpPartyLinks;
            SessionUpParty = session.SessionUpParty;
            DownPartyLinks = session.DownPartyLinks;
            Sub = session.Sub;
            Email = session.Email;
            Phone = session.Phone;
            Username = session.Username;
            CreateTime = session.CreateTime;
            TimeToLive = session.TimeToLive;
        }

        public string Error { get; set; }

        public ActiveSessionViewModel Details { get; set; }
    }
}
