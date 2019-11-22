using BlazorWorker.Core;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public interface IWorkerBackgroundServiceFactory
    {
        Task<IWorker> CreateWebworkerAsync();

        //Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(IWorker webWorkerProxy) where T : class;
    }
}
