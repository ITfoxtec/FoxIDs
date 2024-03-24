﻿using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class ChangePasswordException : AccountException
    {
        public ChangePasswordException() { }
        public ChangePasswordException(string message) : base(message) { }
        public ChangePasswordException(string message, Exception innerException) : base(message, innerException) { }
    }
}