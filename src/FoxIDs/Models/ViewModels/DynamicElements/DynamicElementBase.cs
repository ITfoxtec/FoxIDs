﻿namespace FoxIDs.Models.ViewModels
{
    public class DynamicElementBase
    {
        public virtual string DField1 { get; set; }

        public virtual string DField2 { get; set; }

        public virtual bool Required => false;

        public virtual bool IsUserIdentifier { get; set; }
    }
}
