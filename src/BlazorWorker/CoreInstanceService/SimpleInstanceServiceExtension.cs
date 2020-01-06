using BlazorWorker.Core.SimpleInstanceService;
using MonoWorker.Core;

namespace BlazorWorker.Core.CoreInstanceService
{
    public static class SimpleInstanceServiceExtension
    {
        public static ICoreInstanceService CreateSimpleInstanceService(this IWorkerMessageService source)
        {
            return new CoreInstanceService(new SimpleInstanceServiceProxy(source));
        }
    }
}
