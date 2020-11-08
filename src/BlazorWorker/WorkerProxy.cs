using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
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
                            "System.dll",
                            "System.Core.dll",
                            "System.Buffers.dll",
                            "System.Collections.Concurrent.dll",
                            "System.Collections.Immutable.dll",
                            "System.Collections.NonGeneric.dll",
                            "System.Collections.Specialized.dll",
                            "System.Collections.dll",
                            "System.ComponentModel.Annotations.dll",
                            "System.ComponentModel.DataAnnotations.dll",
                            "System.ComponentModel.EventBasedAsync.dll",
                            "System.ComponentModel.Primitives.dll",
                            "System.ComponentModel.TypeConverter.dll",
                            "System.ComponentModel.dll",
                            "System.Configuration.dll",
                            "System.Console.dll",
                            "System.Core.dll",
                            "System.Data.Common.dll",
                            "System.Data.DataSetExtensions.dll",
                            "System.Data.dll",
                            "System.Diagnostics.Contracts.dll",
                            "System.Diagnostics.Debug.dll",
                            "System.Diagnostics.DiagnosticSource.dll",
                            "System.Diagnostics.FileVersionInfo.dll",
                            "System.Diagnostics.Process.dll",
                            "System.Diagnostics.StackTrace.dll",
                            "System.Diagnostics.TextWriterTraceListener.dll",
                            "System.Diagnostics.Tools.dll",
                            "System.Diagnostics.TraceSource.dll",
                            "System.Diagnostics.Tracing.dll",
                            "System.Drawing.Primitives.dll",
                            "System.Drawing.dll",
                            "System.Dynamic.Runtime.dll",
                            "System.Formats.Asn1.dll",
                            "System.Globalization.Calendars.dll",
                            "System.Globalization.Extensions.dll",
                            "System.Globalization.dll",
                            "System.IO.Compression.Brotli.dll",
                            "System.IO.Compression.FileSystem.dll",
                            "System.IO.Compression.ZipFile.dll",
                            "System.IO.Compression.dll",
                            "System.IO.FileSystem.AccessControl.dll",
                            "System.IO.FileSystem.DriveInfo.dll",
                            "System.IO.FileSystem.Primitives.dll",
                            "System.IO.FileSystem.Watcher.dll",
                            "System.IO.FileSystem.dll",
                            "System.IO.IsolatedStorage.dll",
                            "System.IO.MemoryMappedFiles.dll",
                            "System.IO.Pipelines.dll",
                            "System.IO.Pipes.AccessControl.dll",
                            "System.IO.Pipes.dll",
                            "System.IO.UnmanagedMemoryStream.dll",
                            "System.IO.dll",
                            "System.Linq.Expressions.dll",
                            "System.Linq.Parallel.dll",
                            "System.Linq.Queryable.dll",
                            "System.Linq.dll",
                            "System.Memory.dll",
                            "System.Net.Http.Json.dll",
                            "System.Net.Http.dll",
                            "System.Net.HttpListener.dll",
                            "System.Net.Mail.dll",
                            "System.Net.NameResolution.dll",
                            "System.Net.NetworkInformation.dll",
                            "System.Net.Ping.dll",
                            "System.Net.Primitives.dll",
                            "System.Net.Requests.dll",
                            "System.Net.Security.dll",
                            "System.Net.ServicePoint.dll",
                            "System.Net.Sockets.dll",
                            "System.Net.WebClient.dll",
                            "System.Net.WebHeaderCollection.dll",
                            "System.Net.WebProxy.dll",
                            "System.Net.WebSockets.Client.dll",
                            "System.Net.WebSockets.dll",
                            "System.Net.dll",
                            "System.Numerics.Vectors.dll",
                            "System.Numerics.dll",
                            "System.ObjectModel.dll",
                            "System.Private.CoreLib.dll",
                            "System.Private.DataContractSerialization.dll",
                            "System.Private.Runtime.InteropServices.JavaScript.dll",
                            "System.Private.Uri.dll",
                            "System.Private.Xml.Linq.dll",
                            "System.Private.Xml.dll",
                            "System.Reflection.DispatchProxy.dll",
                            "System.Reflection.Emit.ILGeneration.dll",
                            "System.Reflection.Emit.Lightweight.dll",
                            "System.Reflection.Emit.dll",
                            "System.Reflection.Extensions.dll",
                            "System.Reflection.Metadata.dll",
                            "System.Reflection.Primitives.dll",
                            "System.Reflection.TypeExtensions.dll",
                            "System.Reflection.dll",
                            "System.Resources.Reader.dll",
                            "System.Resources.ResourceManager.dll",
                            "System.Resources.Writer.dll",
                            "System.Runtime.CompilerServices.Unsafe.dll",
                            "System.Runtime.CompilerServices.VisualC.dll",
                            "System.Runtime.Extensions.dll",
                            "System.Runtime.Handles.dll",
                            "System.Runtime.InteropServices.RuntimeInformation.dll",
                            "System.Runtime.InteropServices.dll",
                            "System.Runtime.Intrinsics.dll",
                            "System.Runtime.Loader.dll",
                            "System.Runtime.Numerics.dll",
                            "System.Runtime.Serialization.Formatters.dll",
                            "System.Runtime.Serialization.Json.dll",
                            "System.Runtime.Serialization.Primitives.dll",
                            "System.Runtime.Serialization.Xml.dll",
                            "System.Runtime.Serialization.dll",
                            "System.Runtime.dll",
                            //"System.Security.AccessControl.dll",
                            //"System.Security.Claims.dll",
                            //"System.Security.Cryptography.Algorithms.dll",
                            //"System.Security.Cryptography.Cng.dll",
                            //"System.Security.Cryptography.Csp.dll",
                            //"System.Security.Cryptography.Encoding.dll",
                            //"System.Security.Cryptography.OpenSsl.dll",
                            //"System.Security.Cryptography.Primitives.dll",
                            //"System.Security.Cryptography.X509Certificates.dll",
                            //"System.Security.Principal.Windows.dll",
                            //"System.Security.Principal.dll",
                            //"System.Security.SecureString.dll",
                            "System.Security.dll",
                            //"System.ServiceModel.Web.dll",
                            "System.ServiceProcess.dll",
                            "System.Text.Encoding.CodePages.dll",
                            "System.Text.Encoding.Extensions.dll",
                            "System.Text.Encoding.dll",
                            //"System.Text.Encodings.Web.dll",
                            //"System.Text.Json.dll",
                            //"System.Text.RegularExpressions.dll",
                            "System.Threading.Channels.dll",
                            "System.Threading.Overlapped.dll",
                            "System.Threading.Tasks.Dataflow.dll",
                            "System.Threading.Tasks.Extensions.dll",
                            "System.Threading.Tasks.Parallel.dll",
                            "System.Threading.Tasks.dll",
                            "System.Threading.Thread.dll",
                            "System.Threading.ThreadPool.dll",
                            "System.Threading.Timer.dll",
                            "System.Threading.dll",
                            //"System.Transactions.Local.dll",
                        },
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
            await jsRuntime.InvokeVoidAsync("BlazorWorker.postMessage", this.Identifier, message);
        }

        public long Identifier { get; }
    }
}
