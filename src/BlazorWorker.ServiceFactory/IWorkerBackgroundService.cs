using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public interface IWorkerBackgroundService<T> where T : class
    {
        Task<TResult> RunAsync<TResult>(Expression<Func<T, TResult>> function);
        Task RunAsync(Expression<Action<T>> action);

        bool IsInitialized { get; }
    }
}
