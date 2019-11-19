using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public interface IWorkerBackgroundService<T> where T : class
    {
        Task<TResult> InvokeAsync<TResult>(Expression<Func<T, TResult>> function);
        Task InvokeVoidAsync(Expression<Action<T>> action);
    }
}
