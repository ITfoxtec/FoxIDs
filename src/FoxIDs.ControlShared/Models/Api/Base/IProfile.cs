namespace FoxIDs.Models.Api
{
    public interface IProfile : INameValue, INewNameValue
    {
        string DisplayName { get; set; }
    }
}
