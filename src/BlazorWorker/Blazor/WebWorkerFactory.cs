using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public class WebWorkerFactory : IWebWorkerFactory
    {
        private readonly IJSRuntime jsRuntime;
        private readonly WebWorkerOptions options;

        public WebWorkerFactory(IJSRuntime jsRuntime)
        {
            this.options = new WebWorkerOptions();
            this.jsRuntime = jsRuntime;
        }

        public async Task<IWebWorkerProxy> CreateAsync()
        {
            var worker = new WebWorkerProxy(options, jsRuntime);
            await worker.InitAsync();
            return worker;
        }
    }
}
