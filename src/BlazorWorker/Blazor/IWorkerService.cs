using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public interface IWorkerService<T> where T : class
    {
        Task<TResult> InvokeAsync<TResult>(Expression<Func<T, TResult>> action);
        Task InvokeVoidAsync(Expression<Action<T>> action);
    }
}
