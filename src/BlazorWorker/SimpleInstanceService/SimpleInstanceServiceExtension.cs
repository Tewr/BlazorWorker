using MonoWorker.Core;
using MonoWorker.Core.SimpleInstanceService;

namespace BlazorWorker.Core.SimpleInstanceService
{
    public static class SimpleInstanceServiceExtension
    {
        public static ISimpleInstanceService CreateSimpleInstanceService(this IWorkerMessageService source)
        {
            return new SimpleInstanceServiceProxy(source);
        }
    }


}
