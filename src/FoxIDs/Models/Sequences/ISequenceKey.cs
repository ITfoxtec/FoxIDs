using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Sequences
{
    public interface ISequenceKey : ISequenceData
    {
        string KeyName { get; set; }

        long KeyValidUntil { get; set; }

        bool KeyUsed { get; set; }
    }
}
