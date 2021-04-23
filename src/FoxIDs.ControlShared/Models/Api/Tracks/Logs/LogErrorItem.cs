//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace FoxIDs.Models.Api
//{
//    public class LogErrorItem : LogItem
//    {
//        private readonly LogItemTypes logItemType;

//        public LogErrorItem(LogItemTypes logItemType)
//        {
//            switch (logItemType)
//            {
//                case LogItemTypes.Warning:
//                case LogItemTypes.Error:
//                case LogItemTypes.CriticalError:
//                    this.logItemType = logItemType;
//                    break;
//                default:
//                    throw new InvalidOperationException($"Invalid log item type '{logItemType}'.");
//            }
//        }

//        public override LogItemTypes LogItemType => logItemType;

//        public string Message { get; set; }
//    }
//}
