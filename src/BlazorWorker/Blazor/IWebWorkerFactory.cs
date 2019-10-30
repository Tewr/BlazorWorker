using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public interface IWebWorkerFactory
    {
        Task<IWebWorkerProxy> CreateAsync();
    }
}
