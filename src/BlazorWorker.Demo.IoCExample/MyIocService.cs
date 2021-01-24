using BlazorWorker.WorkerCore;
using System;
using System.Threading.Tasks;

namespace BlazorWorker.Demo.IoCExample
{
    public class MyIocService 
    {
        private int FiveCalledCounter = 0;

        public MyIocService(IWorkerMessageService workerMessageService, IMyServiceDependency aServiceDependency)
        {
            WorkerMessageService = workerMessageService;
            AServiceDependency = aServiceDependency;
        }

        public async Task<int> Five()
        {
            this.FiveCalled?.Invoke(this, FiveCalledCounter++);
            try
            {
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
    }
}
