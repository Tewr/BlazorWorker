using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    public class BlazorWorkerJSRuntime : IJSRuntime, IJSInProcessRuntime
    {

        public IBlazorWorkerJSRuntimeSerializer Serializer { get; set; }

        public BlazorWorkerJSRuntime(IBlazorWorkerJSRuntimeSerializer serializer)
        {
            this.Serializer = serializer;
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
            return Serializer.Serialize(obj);
        }

        private T Deserialize<T>(string serializedObject)
        {
            if (serializedObject is null)
            {
                return default;
            }

            return Serializer.Deserialize<T>(serializedObject);
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
                if (!JSInvokeService.Invoke<bool>("hasOwnProperty", "BlazorWorkerJSRuntimeSerializer"))
                {
                    JSInvokeService.ImportLocalScripts("_content/BlazorWorker.Extensions.JSRuntime/BlazorWorkerJSRuntime.js");
                }

                resultObj = await JSInvokeService.InvokeAsync(identifier,
                    cancellationToken, serializedArgs, "BlazorWorkerJSRuntimeSerializer") as string;
            }
            catch (System.AggregateException e) when (e.InnerException is JSInvokeException)
            {
                throw e.InnerException;
            }

            var result = Deserialize<TValue>(resultObj);
            return result;
        }

        public static string InvokeMethod(string objectInstanceId, string argsString)
        {
            var obj = DotNetObjectReferenceTracker.GetObjectReference(long.Parse(objectInstanceId));
            var serializer = DotNetObjectReferenceTracker.GetCallbackJSRuntime(obj).Serializer;
            var callBackArgs = serializer.Deserialize<CallBackArgs>(argsString);
            var method = obj.GetType().GetMethod(
                callBackArgs.Method, 
                callBackArgs.MethodArgs.Select(arg => arg.GetType()).ToArray());

            var resultObj = method.Invoke(obj, callBackArgs.MethodArgs);
            if (resultObj is null)
            {
                return null;
            }

            return serializer.Serialize(resultObj);
        }

        public class CallBackArgs { 
            public string Method { get; set; }
            public object[] MethodArgs { get; set; }
        }
    }
}
