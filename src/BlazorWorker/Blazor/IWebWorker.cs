using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public interface IWebWorkerProxy : IDisposable
    {
        //Task<IWorkerService<T>> CreateInstanceAsync<T>() where T : class;

        int Identifier { get; }
    }
}
