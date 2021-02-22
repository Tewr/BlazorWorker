using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    public class JSInvokeService
    {
        private static readonly DOMObject self = DOMObject.Self;

        private static readonly TaskRegister selfInvokeRegister = new TaskRegister();

        public static T Invoke<T>(string method, params object[] args)
        {
            return (T)self.Invoke(method, args);
        }

        public static Task InvokeVoidAsync(string method, string serializedArgs)
        {
            return InvokeAsync(method, serializedArgs);
        }

        public static async Task<object> InvokeAsync(
            string method, 
            CancellationToken cancellationToken, 
            string serializedArgs, 
            string serializer = "nativejson")
        {
            var (invokeId, task) = selfInvokeRegister.CreateAndAdd();
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(()=> {
                    task.SetCanceled();
                    selfInvokeRegister.TryRemove(invokeId, out var _);
                });
            }

            self.Invoke("beginInvokeAsync", serializer, invokeId.ToString(), method, serializedArgs);
            return await task.Task;
        }

        public static Task<object> InvokeAsync(string method, string serializedArgs,
            string serializer = "nativejson")
        {
            return InvokeAsync(method, CancellationToken.None, serializedArgs, serializer);
        }

        public static void EndInvokeCallBack(long id, bool isError, object result)
        {
            
            if (!selfInvokeRegister.TryRemove(id, out var taskCompletionSource))
            {
#if DEBUG
                Console.WriteLine($"{nameof(JSInvokeService)}.{nameof(EndInvokeCallBack)}: unknown task id {id}");
#endif
                return;
            }

            // TODO: Error management
            if (!isError)
            {
                taskCompletionSource.SetResult(result);
            }
            else
            {
                taskCompletionSource.SetException(new JSInvokeException(result.ToString()));
            }
        }
    }
}
