using Microsoft.JSInterop;
using MonoWorker.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Map = System.Collections.Generic.Dictionary<string, string>;
namespace BlazorWorker.Core
{
    [DependencyHint(typeof(MessageService))]
    public class WebWorkerProxy : IWebWorkerProxy
    {
        private static readonly IReadOnlyDictionary<string, string> escapeScriptTextReplacements =
            new Dictionary<string, string> { { @"\", @"\\" }, { "\r", @"\r" }, { "\n", @"\n" }, { "'", @"\'" }, { "\"", @"\""" } };
        private readonly IJSRuntime jsRuntime;
        
        private static readonly object idSourceLock = new object();
        private static long idSource;
        private static readonly string messageMethod;

        public event EventHandler<string> IncomingMessage;

        static WebWorkerProxy()
        {
            var messageServiceType = typeof(MessageService);
            messageMethod = $"[{messageServiceType.Assembly.GetName().Name}]{messageServiceType.FullName}:{nameof(MessageService.OnMessage)}";
        }

        public WebWorkerProxy(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            lock (idSourceLock)
            {
                this.Identifier = ++idSource;
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
            this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.Identifier);
        }

        public async Task InitAsync(InitOptions initOptions)
        {
            await InitScript();
            await this.jsRuntime.InvokeVoidAsync(
                "BlazorWorker.initWorker", 
                this.Identifier, 
                DotNetObjectReference.Create(this), 
                new InitOptions {
                    staticAssemblyRefs = 
                        new[] { 
                            "MonoWorker.Core.dll", 
                            "netstandard.dll", 
                            "mscorlib.dll",
                            "WebAssembly.Bindings.dll" },

                    // Hack that works around that documented init procedure of the version of mono.js
                    // delivered with blazor is incompatible with WebAssembly.Bindings 1.0.0.0
                    assemblyRedirectByFilename = new Map { {
                            "WebAssembly.Bindings.dll", "$appRoot$/WebAssembly.Bindings.0.2.2.0.dll"
                    } },
                    callbackMethod = nameof(OnMessage),
                    messageEndPoint = messageMethod //"[MonoWorker.Core]MonoWorker.Core.MessageService:OnMessage"
               }.MergeWith(initOptions));
        }

        

       [JSInvokable]
        public async Task OnMessage(string message)
        {
            IncomingMessage?.Invoke(this, message);
            Console.WriteLine($"{nameof(WebWorkerProxy)}.OnMessage - message: {message}");
        }

        public long Identifier { get; }

        public async Task InitScript()
        {
            if (await IsLoaded())
            {
                return;
            }

            string scriptContent;
            var stream = this.GetType().Assembly.GetManifestResourceStream("BlazorWorker.Core.BlazorWorker.js");
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
