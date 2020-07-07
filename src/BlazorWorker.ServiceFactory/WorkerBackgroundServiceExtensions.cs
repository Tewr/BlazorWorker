using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public static class WorkerBackgroundServiceExtensions
    {
        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy, Action<WorkerInitOptions> workerInitOptionsModifier) where T : class
        {
            var options = new WorkerInitOptions();
            workerInitOptionsModifier(options);
            return await webWorkerProxy.CreateBackgroundServiceAsync<T>(options);
        }

        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy, WorkerInitOptions workerInitOptions = null) where T : class
        {
            var proxy = new WorkerBackgroundServiceProxy<T>(webWorkerProxy, new WebWorkerOptions());
            if (workerInitOptions == null)
            {
                workerInitOptions = new WorkerInitOptions().AddConventionalDependencyFor<T>();
            }

            await proxy.InitAsync(workerInitOptions);
            return proxy;
        }
    }
}

