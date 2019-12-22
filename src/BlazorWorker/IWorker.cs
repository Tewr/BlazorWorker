using MonoWorker.Core;
using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{

    public interface IWorker : IWorkerMessageService, IDisposable
    {
        bool IsInitialized { get; }
        
        long Identifier { get; }

        Task InitAsync(WorkerInitOptions initOptions);
    }
}
