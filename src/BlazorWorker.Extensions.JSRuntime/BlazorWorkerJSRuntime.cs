using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
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

        public BlazorWorkerJSRuntime(): 
            this(new DefaultBlazorWorkerJSRuntimeSerializer())
        {
        }

        public T Invoke<T>(string identifier, params object[] args)
        {
            return JSInvokeService.Invoke<T>(identifier, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? new object[] { });
        }

        private string Serialize(object obj)
        {
            return serializer.Serialize(obj);
        }

        private T Deserialize<T>(string serializedObject)
        {
            if (serializedObject is null)
            {
                return default;
            }

            return serializer.Deserialize<T>(serializedObject);
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, 
            CancellationToken cancellationToken, 
            object[] args)
        {
            var serializedArgs = Serialize(args);
            string resultObj;
            
            try
            {
                resultObj = await JSInvokeService.InvokeAsync(identifier,
                    cancellationToken, serializedArgs) as string;
            }
            catch (System.AggregateException e) when (e.InnerException is JSInvokeException)
            {
                throw e.InnerException;
            }

            var result = Deserialize<TValue>(resultObj);
            return result;
        }
    }
}
