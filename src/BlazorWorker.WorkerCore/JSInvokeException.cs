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

    [Serializable]
    public class WorkerException : Exception
    {
        public WorkerException(string message) : base(message)
        {
        }

        public WorkerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WorkerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}