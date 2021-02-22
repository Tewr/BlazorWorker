using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace BlazorWorker.Demo.IoCExample
{
    public class MyIocService 
    {
        private int FiveCalledCounter = 0;

        public MyIocService(IWorkerMessageService workerMessageService, IMyServiceDependency aServiceDependency, IJSRuntime jSRuntime)
        {
            WorkerMessageService = workerMessageService;
            AServiceDependency = aServiceDependency;
            JSRuntime = jSRuntime;
        }

        public async Task<int> Five()
        {
            this.FiveCalled?.Invoke(this, FiveCalledCounter++);
            try
            {
                var theNumberOfTheBeast = await this.JSRuntime.InvokeAsync<int>("eval", "(function(){ console.log('Hello world invoke call from MyIocService'); return 666; })()");
                Console.WriteLine($"{theNumberOfTheBeast} : The number of the beast");
                return this.AServiceDependency.Five();
            }
            finally
            {
                if (this.FiveCalledCounter > 2)
                {
                    await this.WorkerMessageService.PostMessageAsync($"{nameof(FiveCalledCounter)} has been called more than 2 times: {this.FiveCalledCounter} times!");
                }
            }
        }

        public event EventHandler<int> FiveCalled;

        public IWorkerMessageService WorkerMessageService { get; }

        public IMyServiceDependency AServiceDependency { get; }
        public IJSRuntime JSRuntime { get; }
    }
}
