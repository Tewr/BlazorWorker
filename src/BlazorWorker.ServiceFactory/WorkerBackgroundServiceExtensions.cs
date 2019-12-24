using BlazorWorker.BackgroundServiceFactory.Shared;
using BlazorWorker.Core;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public static class WorkerBackgroundServiceExtensions
    {
        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy, WorkerInitOptions workerInitOptions = null) where T : class
        {
            var proxy = new WorkerBackgroundServiceProxy<T>(webWorkerProxy, new WebWorkerOptions());
            if (workerInitOptions == null)
            {
                workerInitOptions = new WorkerInitOptions()
                {
                    // Takes a (not so) wild guess and sets the dll name to the assembly name
                    DependentAssemblyFilenames = new[] { $"{typeof(T).Assembly.GetName().Name}.dll" }
                };
            }
            await proxy.InitAsync(workerInitOptions);
            return proxy;
        }
    }
}
