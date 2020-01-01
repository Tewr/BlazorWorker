using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public interface IWorkerFactory
    {
        Task<IWorker> CreateAsync();//WorkerInitOptions initOptions);
    }
}
