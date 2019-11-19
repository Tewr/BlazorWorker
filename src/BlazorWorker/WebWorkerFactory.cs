using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public class WebWorkerFactory : IWebWorkerFactory
    {
        private readonly IJSRuntime jsRuntime;
        //private readonly WebWorkerOptions options;

        public WebWorkerFactory(IJSRuntime jsRuntime)
        {
            //this.options = new WebWorkerOptions();
            this.jsRuntime = jsRuntime;
        }

        public async Task<IWebWorkerProxy> CreateAsync(InitOptions initOptions)
        {
            var worker = new WebWorkerProxy(jsRuntime);
            await worker.InitAsync(initOptions);
            return worker;
        }
    }
}
