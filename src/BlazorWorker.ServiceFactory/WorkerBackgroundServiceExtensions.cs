using BlazorWorker.BackgroundServiceFactory.Shared;
using BlazorWorker.Core;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public static class WorkerBackgroundServiceExtensions
    {
        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy) where T : class
        {
            var proxy =  new WorkerBackgroundServiceProxy<T>(webWorkerProxy, new WebWorkerOptions());
            await proxy.InitAsync();
            return proxy;
        }
    }
}
