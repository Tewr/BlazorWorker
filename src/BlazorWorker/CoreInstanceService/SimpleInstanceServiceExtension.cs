using BlazorWorker.Core.SimpleInstanceService;
using BlazorWorker.WorkerCore;

namespace BlazorWorker.Core.CoreInstanceService
{
    public static class SimpleInstanceServiceExtension
    {
        public static ICoreInstanceService CreateSimpleInstanceService(this IWorker source)
        {
            return new CoreInstanceService(new SimpleInstanceServiceProxy(source));
        }
    }
}
