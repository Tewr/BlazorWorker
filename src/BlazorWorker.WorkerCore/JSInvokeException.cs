using System;

namespace BlazorWorker.WorkerCore
{
    public class JSInvokeException : Exception
    {
        public JSInvokeException(string message): base(message)
        {
        }
    }
}