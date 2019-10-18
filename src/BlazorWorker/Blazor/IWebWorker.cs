using System;
using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public interface IWebWorker : IDisposable
    {
        Task<IWorkerService<T>> CreateInstanceAsync<T>() where T : class;
    }
}
