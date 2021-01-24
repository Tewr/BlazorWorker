using BlazorWorker.WorkerCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlazorWorker.Demo.IoCExample
{
    public class MyServiceStartup
    {
        /// <summary>
        /// The constructor uses the built-in injection for library-native services such as the <see cref="IWorkerMessageService"/>.
        /// </summary>
        /// <param name="workerMessageService"></param>
        public MyServiceStartup(IWorkerMessageService workerMessageService)
        {
            WorkerMessageService = workerMessageService;
        }

        private IServiceProvider sc;

        public IWorkerMessageService WorkerMessageService { get; }

        public T Resolve<T>()
        {
            //setup our DI
            if (sc is null) {
                sc = new ServiceCollection()
                    .AddTransient<IMyServiceDependency, MyServiceDependency>()
                    .AddTransient<MyIocService>()
                    .AddSingleton(WorkerMessageService)
                    .BuildServiceProvider();
            }

            return sc.GetService<T>();
        }
    }
}
