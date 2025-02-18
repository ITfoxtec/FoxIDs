using FoxIDs.Models;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PlanException : Exception
    {
        public Plan Plan { get; internal set; }

        public PlanException(Plan plan)
        {
            Plan = plan; 
        }

        public PlanException(Plan plan, string message) : base(message)
        {
            Plan = plan;
        }

        public PlanException(Plan plan, string message, Exception inner) : base(message, inner)
        {
            Plan = plan;
        }
    }
}
