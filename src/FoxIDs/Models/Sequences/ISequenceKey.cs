using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public interface ISequenceKey : ISequenceData
    {
        public string KeyName { get; set; }

        public List<string> KeyNames { get; set; }

        long KeyValidUntil { get; set; }

        bool KeyUsed { get; set; }
    }
}
