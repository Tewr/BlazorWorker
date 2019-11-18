using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public class WebWorkerProxy : IWebWorkerProxy
    {
        private static readonly IReadOnlyDictionary<string, string> escapeScriptTextReplacements =
            new Dictionary<string, string> { { @"\", @"\\" }, { "\r", @"\r" }, { "\n", @"\n" }, { "'", @"\'" }, { "\"", @"\""" } };
        private readonly IJSRuntime jsRuntime;
        private readonly string guid = Guid.NewGuid().ToString("n");
        private static object idSourceLock = new object();
        private static int idSource;
        private readonly int id;


        public WebWorkerProxy(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            lock (idSourceLock)
            {
                this.id = ++idSource;
            }
        }

        /*public async Task<IWorkerService<T>> CreateInstanceAsync<T>() where T : class
        {
            var workerService = new WebWorkerServiceProxy<T>(this.guid, options, jsRuntime);
            await workerService.InitAsync();
            return workerService;
        }*/

        public void Dispose()
        {
            this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.guid);
        }

        public async Task InitAsync()
        {
            await InitScript();
            // Todo : Load BlazorWorker.js from resources
            await this.jsRuntime.InvokeVoidAsync("BlazorWorker.initWorker", this.guid, DotNetObjectReference.Create(this), new { testParam = 1 });
        }

        [JSInvokable]
        public async Task OnMessage(string message)
        {
            Console.WriteLine($"{nameof(WebWorkerProxy)}.OnMessage - message: {message}");
        }

        public int Identifier => id;

        public async Task InitScript()
        {
            if (await IsLoaded())
            {
                return;
            }

            string scriptContent;
            var stream = this.GetType().Assembly.GetManifestResourceStream("BlazorWorker.Blazor.BlazorWorker.js");
            using (stream)
            {
                using (var streamReader = new StreamReader(stream))
                {
                    scriptContent = await streamReader.ReadToEndAsync();
                }
            }

            await ExecuteRawScriptAsync(scriptContent);
            var loaderLoopBreaker = 0;
            while (!await IsLoaded())
            {
                loaderLoopBreaker++;
                await Task.Delay(100);

                // Fail after 3s not to block and hide any other possible error
                if (loaderLoopBreaker > 25)
                {
                    throw new InvalidOperationException("Unable to initialize BlazorWorker.js");
                }
            }
        }
        private async Task<bool> IsLoaded()
        {
            return await jsRuntime.InvokeAsync<bool>("eval", "(function() { return !!window.BlazorWorker })()");
        }
        private async Task ExecuteRawScriptAsync(string scriptContent)
        {
            scriptContent = escapeScriptTextReplacements.Aggregate(scriptContent, (r, pair) => r.Replace(pair.Key, pair.Value));
            var blob = $"URL.createObjectURL(new Blob([\"{scriptContent}\"],{{ \"type\": \"text/javascript\"}}))";
            var bootStrapScript = $"(function(){{var d = document; var s = d.createElement('script'); s.async=false; s.src={blob}; d.head.appendChild(s); d.head.removeChild(s);}})();";
            await jsRuntime.InvokeVoidAsync("eval", bootStrapScript);
        }
    }
}
