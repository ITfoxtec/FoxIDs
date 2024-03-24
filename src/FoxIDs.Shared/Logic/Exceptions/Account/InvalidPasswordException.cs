﻿using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class InvalidPasswordException : AccountException
    {
        public InvalidPasswordException() { }
        public InvalidPasswordException(string message) : base(message) { }
        public InvalidPasswordException(string message, Exception innerException) : base(message, innerException) { }
    }
}