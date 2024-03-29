﻿using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class PasswordEmailTextComplexityException : AccountException
    {
        public PasswordEmailTextComplexityException() { }
        public PasswordEmailTextComplexityException(string message) : base(message) { }
        public PasswordEmailTextComplexityException(string message, Exception innerException) : base(message, innerException) { }
    }
}