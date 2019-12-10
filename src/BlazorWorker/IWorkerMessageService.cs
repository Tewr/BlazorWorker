using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public interface IWorkerMessageService
    {
        event EventHandler<string> IncomingMessage;

        Task PostMessageAsync(string message);
    }
}
