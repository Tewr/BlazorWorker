using MonoWorker.Core;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public interface IWorkerBackgroundService<T> where T : class
    {
        Task<Shared.EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler);
        Task<TResult> RunAsync<TResult>(Expression<Func<T, TResult>> function);
        Task RunAsync(Expression<Action<T>> action);

        IWorkerMessageService GetWorkerMessageService();

        ValueTask DisposeAsync();
    }
}
