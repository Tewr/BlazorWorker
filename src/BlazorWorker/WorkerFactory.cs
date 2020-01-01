using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public class WorkerFactory : IWorkerFactory
    {
        private readonly IJSRuntime jsRuntime;

        public WorkerFactory(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async Task<IWorker> CreateAsync()//WorkerInitOptions initOptions)
        {
            var worker = new WorkerProxy(jsRuntime);
            //await worker.InitAsync(initOptions);
            return worker;
        }
    }
}
