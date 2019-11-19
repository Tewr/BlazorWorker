using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    public interface IWebWorkerFactory
    {
        Task<IWebWorkerProxy> CreateAsync(InitOptions initOptions);
    }
}
