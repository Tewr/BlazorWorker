using System;
using System.Runtime.Serialization;

namespace BlazorWorker.WorkerCore
{
    [Serializable]
    public class JSInvokeException : Exception
    {
        public JSInvokeException(string message): base(message)
        {
        }

        public JSInvokeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected JSInvokeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}