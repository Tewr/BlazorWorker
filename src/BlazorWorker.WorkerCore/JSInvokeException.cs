using System;
using System.Collections.Generic;

namespace BlazorWorker.WorkerCore
{
    public class JSInvokeException : Exception
    {
        public JSInvokeException(string message): base(message)
        {
        }
    }
}