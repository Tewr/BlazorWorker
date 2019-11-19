using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public interface IWebWorkerProxy : IDisposable
    {
        //Task<IWorkerService<T>> CreateInstanceAsync<T>() where T : class;

        long Identifier { get; }

        Task InitAsync(InitOptions initOptions);

        event EventHandler<string> IncomingMessage;
    }
}
