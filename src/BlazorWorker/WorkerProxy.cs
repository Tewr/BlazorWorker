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
    public class WorkerProxy : IWorker
    {
        private static readonly IReadOnlyDictionary<string, string> escapeScriptTextReplacements =
            new Dictionary<string, string> { { @"\", @"\\" }, { "\r", @"\r" }, { "\n", @"\n" }, { "'", @"\'" }, { "\"", @"\""" } };
        private readonly IJSRuntime jsRuntime;
        private readonly ScriptLoader scriptLoader;
        private static readonly object idSourceLock = new object();
        private static long idSource;
        
        /// <summary>
        /// [MonoWorker.Core]MonoWorker.Core.MessageService:OnMessage"
        /// </summary>
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
            // TODO: Is lock overkill here as this is exclusively run on ui thread?
            lock (idSourceLock)
            {
                this.Identifier = ++idSource;
            }
        }

        public void Dispose()
        {
            this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.Identifier);
        }

        public async Task InitAsync(WorkerInitOptions initOptions)
        {
            await this.scriptLoader.InitScript();
            await this.jsRuntime.InvokeVoidAsync(
                "BlazorWorker.initWorker", 
                this.Identifier, 
                DotNetObjectReference.Create(this), 
                new WorkerInitOptions {
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
            Console.WriteLine($"{nameof(WorkerProxy)}.OnMessage - message: {message}");
        }

        public async Task PostMessageAsync(string message)
        {
            await jsRuntime.InvokeVoidAsync("BlazorWorker.postMessage", message);
        }

        public long Identifier { get; }
    }
}
