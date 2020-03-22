using System;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    public interface IWorkerMessageService
    {
        event EventHandler<string> IncomingMessage;

        Task PostMessageAsync(string message);
    }
}
