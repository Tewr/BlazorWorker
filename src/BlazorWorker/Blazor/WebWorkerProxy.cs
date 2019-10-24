using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public class WebWorkerProxy : IWebWorker
    {
        private readonly WebWorkerOptions options;
        private readonly IJSRuntime jsRuntime;
        private readonly string guid = Guid.NewGuid().ToString("n");

        public WebWorkerProxy(WebWorkerOptions options, IJSRuntime jsRuntime)
        {
            this.options = options;
            this.jsRuntime = jsRuntime;
        }

        public async Task<IWorkerService<T>> CreateInstanceAsync<T>() where T : class
        {
            var workerService = new WebWorkerServiceProxy<T>(this.guid, options, jsRuntime);
            await workerService.InitAsync();
            return workerService;
        }

        public void Dispose()
        {
            this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.guid);
        }

        internal async Task InitAsync()
        {
            // Todo : Load BlazorWorker.js from resources
            await this.jsRuntime.InvokeVoidAsync("BlazorWorker.initWorker", this.guid);
        }

        public async Task OnMessage(int id, string message)
        {
            Console.WriteLine($"id: {id} message: {message}");
        }
    }
}
