//using System;
//using System.Runtime.Serialization;

//namespace FoxIDs.Logic
//{
//    [Serializable]
//    public class _AccountException : Exception, IViewErrorMessage
//    {
//        public ViewErrorMessages ViewErrorMessage { get; protected set; }

//        public AccountException() { }

//        public AccountException(string message, ViewErrorMessages viewErrorMessage) : base(message)
//        {
//            ViewErrorMessage = viewErrorMessage;
//        }

//        public AccountException(string message, ViewErrorMessages viewErrorMessage, Exception innerException) : base(message, innerException)
//        {
//            ViewErrorMessage = viewErrorMessage;
//        }

//        protected AccountException(SerializationInfo info, StreamingContext context) : base(info, context) { }

//        public override string Message => $"{base.Message} [View message: {ViewErrorMessage}]";
//    }
//}