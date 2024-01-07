using BlazorWorker.WorkerCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;

using BlazorWorker.Extensions.JSRuntime;

namespace BlazorWorker.Demo.IoCExample
{
    public class MyServiceStartup
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWorkerMessageService workerMessageService;

        /// <summary>
        /// The constructor uses the built-in injection for library-native services such as the <see cref="IWorkerMessageService"/>.
        /// </summary>
        /// <param name="workerMessageService"></param>
        public MyServiceStartup(IWorkerMessageService workerMessageService)
        {
            this.workerMessageService = workerMessageService;
            serviceProvider = ServiceCollectionHelper.BuildServiceProviderFromMethod(Configure);
        }

        public T Resolve<T>() =>  serviceProvider.GetService<T>();

        public void Configure(IServiceCollection services)
        {
            services.AddTransient<IMyServiceDependency, MyServiceDependency>()
                    .AddBlazorWorkerJsRuntime()
                    .AddTransient<MyIocService>()
                    .AddSingleton(workerMessageService);
        }
    }
}
