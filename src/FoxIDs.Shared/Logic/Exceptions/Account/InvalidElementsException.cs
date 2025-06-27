using System;
using System.Collections.Generic;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidElementsException : AccountException
    {
        public InvalidElementsException() { }
        public InvalidElementsException(string message) : base(message) { }
        public InvalidElementsException(string message, Exception innerException) : base(message, innerException) { }

        public List<ElementError> Elements { get; set; }
    }
}