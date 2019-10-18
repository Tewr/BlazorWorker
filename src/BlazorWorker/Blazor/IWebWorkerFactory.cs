using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public interface IWebWorkerFactory
    {
        Task<IWebWorker> CreateAsync<T>() where T : class;
    }
}
