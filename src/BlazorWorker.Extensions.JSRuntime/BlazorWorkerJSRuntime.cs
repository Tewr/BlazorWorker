using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    /// <summary>
    /// IJSRuntime implementation for use in a worker process
    /// </summary>
    public partial class BlazorWorkerJSRuntime : IJSRuntime, IJSInProcessRuntime
    {
        private static bool isJsInitialized;

        /// <summary>
        /// Serializer that will be used
        /// </summary>
        public IBlazorWorkerJSRuntimeSerializer Serializer { get; set; }

        /// <summary>
        /// The serializer options to be used for the underlying serializer
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; }
        
        /// <summary>
        /// Creates a new JSRuntime
        /// </summary>
        public BlazorWorkerJSRuntime()
        {

            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    { new DotNetObjectReferenceJsonConverterFactory(this) }
}
            };

            this.Serializer = new DefaultBlazorWorkerJSRuntimeSerializer(SerializerOptions);
        }

        /// <summary>
        /// Invokes a method defined on the worker globalThis (self) object
        /// </summary>
        /// <typeparam name="T">expected return type</typeparam>
        /// <param name="identifier">js method name</param>
        /// <param name="args">JSON serializable arguments to send to the js method</param>
        /// <returns></returns>
        public T Invoke<T>(string identifier, params object[] args)
        {
            var resultString = JSInvokeService.WorkerInvoke<T>(identifier, Serialize(args));

            return this.Serializer.Deserialize<T>(resultString);
        }

        /// <summary>
        /// Invokes a method defined on the worker globalThis (self) object asynchronically
        /// </summary>
        /// <typeparam name="TValue">expected return type</typeparam>
        /// <param name="identifier">js method name</param>
        /// <param name="args">JSON serializable arguments to send to the js method</param>
        /// <returns></returns>
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

            await EnsureInitialized();
                
            resultObj = await JSInvokeService.WorkerInvokeAsync<TValue>(identifier, serializedArgs);
            
            cancellationToken.ThrowIfCancellationRequested();

            var result = Deserialize<TValue>(resultObj);
            return result;
        }

        private static async Task EnsureInitialized()
        {
            if (!isJsInitialized && !JSInvokeService.IsObjectDefined("BlazorWorkerJSRuntimeSerializer"))
            {
                await JSInvokeService.ImportLocalScripts("_content/Tewr.BlazorWorker.Extensions.JSRuntime/BlazorWorkerJSRuntime.js");
                isJsInitialized = true;
            }
        }

        [JSExport]
        public static string InvokeMethod(string objectInstanceId, string argsString)
        {
#if DEBUG
            Console.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}({objectInstanceId}, {argsString})");
#endif
            try
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
            catch (Exception e)
            {
                Console.Error.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}({objectInstanceId}, {argsString}) error: {e.ToString()}");
                throw;
            }
        }

        public class CallBackArgs { 
            public string Method { get; set; }
            public object[] MethodArgs { get; set; }
        }
    }
}
