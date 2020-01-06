using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.Core.CoreInstanceService
{

    public class WorkerInstanceInitializeException : WorkerException
    {
        public WorkerInstanceInitializeException(string message, string fullMessage)
            :base($"Error when initializing instance: {message}", fullMessage)
        {
        }
    }
}
