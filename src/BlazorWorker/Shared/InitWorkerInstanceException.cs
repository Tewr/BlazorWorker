using System;
using System.Runtime.Serialization;

namespace BlazorWorker.Worker
{
    [Serializable]
    internal class InitWorkerInstanceException : Exception
    {
        public InitWorkerInstanceException()
        {
        }

        public InitWorkerInstanceException(string message) : base(message)
        {
        }

        public InitWorkerInstanceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InitWorkerInstanceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}