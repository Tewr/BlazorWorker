using System;
using System.Threading.Tasks;

namespace MonoWorker.Core
{
    public interface IWorkerMessageService
    {
        event EventHandler<string> IncomingMessage;

        Task PostMessageAsync(string message);
    }
}
