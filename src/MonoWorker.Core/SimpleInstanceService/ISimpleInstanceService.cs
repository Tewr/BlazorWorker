using System.Threading.Tasks;

namespace MonoWorker.Core.SimpleInstanceService
{
    public interface ISimpleInstanceService
    {
        Task<DisposeResult> DisposeInstance(DisposeInstanceRequest request);
        Task<InitInstanceResult> InitInstance(InitInstanceRequest initInstanceRequest);
    }
}