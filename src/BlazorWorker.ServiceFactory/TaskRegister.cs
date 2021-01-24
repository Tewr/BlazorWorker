using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    internal class TaskRegister<TMessage> : Dictionary<long, TaskCompletionSource<TMessage>>
    {
        public (long, TaskCompletionSource<TMessage>) CreateAndAdd()
        {
            var tcs = new TaskCompletionSource<TMessage>();
            var id = ++TaskRegister.idSource;
            this.Add(id, tcs);
            return (id, tcs);
        }
    }

    internal class TaskRegister : TaskRegister<bool> {
        public static long idSource;
    }
}
