using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class LogicSequenceBase : LogicBase
    {
        public LogicSequenceBase(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public Sequence Sequence => HttpContext.GetSequence();
    }
}
