using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
namespace BlazorWorker.Core
{
    [DependencyHint(typeof(MessageService))]
    public class WorkerProxy : IWorker
    {
        private readonly IJSRuntime jsRuntime;
        private readonly ScriptLoader scriptLoader;
        private static long idSource;
        private bool isDisposed = false;
        private static readonly string messageMethod;
        private readonly DotNetObjectReference<WorkerProxy> thisReference;


        public event EventHandler<string> IncomingMessage;
        public bool IsInitialized { get; set; }
        static WorkerProxy()
        {
            var messageServiceType = typeof(MessageService);
            messageMethod = MonoTypeHelper.GetStaticMethodId<MessageService>(nameof(MessageService.OnMessage));
            //$"[{messageServiceType.Assembly.GetName().Name}]{messageServiceType.FullName}:{nameof(MessageService.OnMessage)}";
        }

        public WorkerProxy(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            this.scriptLoader = new ScriptLoader(this.jsRuntime);
            this.Identifier = ++idSource;
            thisReference = DotNetObjectReference.Create(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!isDisposed)
            {
                await this.jsRuntime.InvokeVoidAsync("BlazorWorker.disposeWorker", this.Identifier);
                thisReference.Dispose();
                isDisposed = true;
            }
        }

        public async Task InitAsync(WorkerInitOptions initOptions)
        {
            await this.scriptLoader.InitScript();

            await this.jsRuntime.InvokeVoidAsync(
                "BlazorWorker.initWorker", 
                this.Identifier, 
                thisReference,
                new WorkerInitOptions {
                    DependentAssemblyFilenames = 
                       WorkerProxyDependencies.DependentAssemblyFilenames,
                    CallbackMethod = nameof(OnMessage),
                    MessageEndPoint = messageMethod
               }.MergeWith(initOptions));
        }

        [JSInvokable]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task OnMessage(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            IncomingMessage?.Invoke(this, message);
        }

        public async Task PostMessageAsync(string message)
        {
            var arg = new PostMessageArg() { Identifier = this.Identifier, Message = message, ByteArray = new byte[] {1,2,3 } };
#if NET5_0
            (jsRuntime as IJSUnmarshalledRuntime).InvokeUnmarshalled<PostMessageArg, object>(
                "BlazorWorker.postMessage", arg);
#else
            await jsRuntime.InvokeVoidAsync("BlazorWorker.postMessage", this.Identifier, message);
#endif
        }

        public long Identifier { get; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PostMessageArg
    {
        [FieldOffset(0)]
        public long Identifier;
        [FieldOffset(8)]
        public string Message;
        [FieldOffset(12)]
        public byte[] ByteArray;
    }
}
