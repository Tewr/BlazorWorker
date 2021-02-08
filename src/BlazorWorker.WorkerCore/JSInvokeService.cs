using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    public class JSInvokeService
    {
        private static readonly DOMObject self = DOMObject.Self;

        private static readonly TaskRegister selfInvokeRegister = new TaskRegister();

        public static T SelfInvoke<T>(string method, params object[] args)
        {
            return (T)self.Invoke(method, args);
        }

        public static Task SelfInvokeVoidAsync(string method, params object[] args)
        {
            return SelfInvokeAsync(method, args);
        }

        public static async Task<object> SelfInvokeAsync(string method, CancellationToken cancellationToken, params object[] args)
        {
            var (id, task) = selfInvokeRegister.CreateAndAdd();
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(()=> {
                    task.SetCanceled();
                    selfInvokeRegister.TryRemove(id, out var _);
                });
            }
            self.Invoke("selfInvokeAsync", new object[] { id, method }.Concat(args));
            return await task.Task;
        }

        public static async Task<object> SelfInvokeAsync(string method, params object[] args)
        {
            return SelfInvokeAsync(method, CancellationToken.None, args);
        }

        public static void SelfInvokeCallBack(long id, bool isError, object result)
        {
            
            if (!selfInvokeRegister.TryRemove(id, out var taskCompletionSource))
            {
#if DEBUG
                Console.WriteLine($"{nameof(JSInvokeService)}.{nameof(SelfInvokeCallBack)}: unknown task id {id}");
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
                taskCompletionSource.SetException(new Exception(result.ToString()));
            }
        }
    }
}
