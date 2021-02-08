using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    public class BlazorWorkerJSRuntime : IJSRuntime, IJSInProcessRuntime
    {
        private readonly IBlazorWorkerJSRuntimeSerializer serializer;

        public BlazorWorkerJSRuntime(IBlazorWorkerJSRuntimeSerializer serializer)
        {
            this.serializer = serializer;
        }

        public BlazorWorkerJSRuntime(): this(new DefaultBlazorWorkerJSRuntimeSerializer())
        {
        }

        public T Invoke<T>(string identifier, params object[] args)
        {
            return JSInvokeService.SelfInvoke<T>(identifier, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        private string[] SerializeArgs(object[] args)
        {
            return args.Select(Serialize).ToArray();
        }

        private string Serialize(object obj)
        {
            return serializer.Serialize(obj);
        }

        private T Deserialize<T>(string serializedObject)
        {
            return serializer.Deserialize<T>(serializedObject);
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            var serializedArgs = SerializeArgs(args);
            // Two arguments are sent: Method 
            var allArgs = new object[] { identifier }.Concat(serializedArgs);
            var resultObj = await JSInvokeService.SelfInvokeAsync("selfInvokeJsonAsync", cancellationToken, allArgs) as string;
            var result = Deserialize<TValue>(resultObj);
            return result;
        }
    }
}
