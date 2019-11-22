using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public interface IWorker : IDisposable
    {
        bool IsInitialized { get; }
        
        long Identifier { get; }

        Task InitAsync(WorkerInitOptions initOptions);

        event EventHandler<string> IncomingMessage;

        Task PostMessageAsync(string message);
    }
}
