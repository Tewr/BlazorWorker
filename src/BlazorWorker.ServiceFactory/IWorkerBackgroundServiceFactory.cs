using BlazorWorker.Core;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public interface IWorkerBackgroundServiceFactory
    {
        Task<IWebWorkerProxy> CreateWebworker();

        Task<IWorkerBackgroundService<T>> CreateBackgroundService<T>() where T : class;
    }
}
