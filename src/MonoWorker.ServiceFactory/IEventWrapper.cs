using System;

namespace MonoWorker.BackgroundServiceHost
{
    public interface IEventWrapper
    {
        long InstanceId { get; }
        long EventHandleId { get; }
        Action Unregister { get; set; }
    }

}
