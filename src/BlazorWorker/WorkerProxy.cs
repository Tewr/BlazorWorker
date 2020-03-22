using Microsoft.JSInterop;
using BlazorWorker.WorkerCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace BlazorWorker.Core
{
    [DependencyHint(typeof(MessageService))]
    
    public class WorkerProxy : IWorker
    {
        private static readonly IReadOnlyDictionary<string, string> escapeScriptTextReplacements =
            new Dictionary<string, string> { { @"\", @"\\" }, { "\r", @"\r" }, { "\n", @"\n" }, { "'", @"\'" }, { "\"", @"\""" } };
        private readonly IJSRuntime jsRuntime;
        private readonly ScriptLoader scriptLoader;
        private static long idSource;
        private bool isDisposed = false;
        private static readonly string messageMethod;

        public event EventHandler<string> IncomingMessage;
        public bool IsInitialized { get; private set; }
        static WorkerProxy()
        {
            var messageServiceType = typeof(MessageService);
            messageMethod = $"[{messageServiceType.Assembly.GetName().Name}]{messageServiceType.FullName}:{nameof(MessageService.OnMessage)}";
        }

        public WorkerProxy(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            this.scriptLoader = new ScriptLoader(this.jsRuntime);
            this.Identifier = ++idSource;
        }

        public async ValueTask DisposeAsync()
        {
            if (!isDisposed)
    
            {
                await this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.Identifier);
                isDisposed = true;
            }
        }

        public async Task InitAsync(WorkerInitOptions initOptions)
        {
            await this.scriptLoader.InitScript();

            await this.jsRuntime.InvokeVoidAsync(
                "BlazorWorker.initWorker", 
                this.Identifier, 
                DotNetObjectReference.Create(this), 
                new WorkerInitOptions {
                    DependentAssemblyFilenames = 
                        new[] { 
                            "BlazorWorker.WorkerCore.dll", 
                            "netstandard.dll",
                            "mscorlib.dll",
                            "WebAssembly.Bindings.dll",
                            "System.dll",
                            "System.Core.dll"
                        },
                    CallbackMethod = nameof(OnMessage),
                    MessageEndPoint = messageMethod
               }.MergeWith(initOptions));
        }

        [JSInvokable]
        public async Task OnMessage(string message)
        {
            IncomingMessage?.Invoke(this, message);
        }

        public async Task PostMessageAsync(string message)
        {
            await jsRuntime.InvokeVoidAsync("BlazorWorker.postMessage", this.Identifier, message);
        }

        public long Identifier { get; }
    }
}
