using System;
using System.Threading.Tasks;

namespace BlazorWorker.Core.CoreInstanceService
{
    public interface ICoreInstanceService
    {
        Task<IInstanceHandle> CreateInstance(Type type);
        Task<IInstanceHandle> CreateInstance<T>();
    }

    public interface IInstanceHandle {

        ValueTask DisposeAsync();
    }
}
