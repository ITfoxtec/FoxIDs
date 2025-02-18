using System.Collections.Generic;

namespace FoxIDs.Models
{
    public interface IDataElement
    {
        string Id { get; set; }
        List<string> AdditionalIds { get; set; }
        string DataType { get; set; }
    }
}
