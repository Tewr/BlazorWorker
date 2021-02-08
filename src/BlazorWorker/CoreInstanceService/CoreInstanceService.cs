using System;
using System.Threading.Tasks;
using BlazorWorker.Core.SimpleInstanceService;
using BlazorWorker.WorkerCore;
using BlazorWorker.WorkerCore.SimpleInstanceService;

namespace BlazorWorker.Core.CoreInstanceService
{
    using WorkerSimpleInstanceService = BlazorWorker.WorkerCore.SimpleInstanceService.SimpleInstanceService;
    internal class CoreInstanceService : ICoreInstanceService
    {
        private static long sourceId;
        private readonly SimpleInstanceServiceProxy simpleInstanceServiceProxy;
        private readonly static string initEndpointID;
        private readonly static string selfInvokeCallBackEndpointID;

        static CoreInstanceService()
        {
            initEndpointID = 
                MonoTypeHelper.GetStaticMethodId<WorkerSimpleInstanceService>(nameof(WorkerSimpleInstanceService.Init));
            selfInvokeCallBackEndpointID = 
                MonoTypeHelper.GetStaticMethodId<JSInvokeService>(nameof(JSInvokeService.SelfInvokeCallBack));
        }

        public CoreInstanceService(SimpleInstanceServiceProxy simpleInstanceServiceProxy)
        {
            this.simpleInstanceServiceProxy = simpleInstanceServiceProxy;
        }
        public Task<IInstanceHandle> CreateInstance<T>()
        {
            return CreateInstance(typeof(T));
        }

        public Task<IInstanceHandle> CreateInstance<T>(Action<WorkerInitOptions> workerInitOptionsModifier)
        {
            return CreateInstance(typeof(T), workerInitOptionsModifier);
        }

        public Task<IInstanceHandle> CreateInstance<T>(WorkerInitOptions workerInitOptions)
        {
            return CreateInstance(typeof(T), workerInitOptions);
        }

        public async Task<IInstanceHandle> CreateInstance(Type t)
        {
            return await CreateInstance(t, (WorkerInitOptions)null);
        }

        public async Task<IInstanceHandle> CreateInstance(Type t, Action<WorkerInitOptions> workerInitOptionsModifier)
        {
            var options = new WorkerInitOptions();
            workerInitOptionsModifier(options);
            return await CreateInstance(t, options);
        }

        public async Task<IInstanceHandle> CreateInstance(Type t, WorkerInitOptions options)
        {
            var id = ++sourceId;
            if (!this.simpleInstanceServiceProxy.IsInitialized)
            {
                if (options == null)
                {
                    options = new WorkerInitOptions();
                    options.AddAssemblyOfType(t);
                }

                if (options.UseConventionalServiceAssembly)
                {
                    options.AddAssemblyOfType(t);
                }

                await this.simpleInstanceServiceProxy.InitializeAsync(
                    new WorkerInitOptions
                    {
                        InitEndPoint = initEndpointID,
                        SelfInvokeCallBackEndpoint = selfInvokeCallBackEndpointID
                    }.MergeWith(options)); ;
            }
            var initResult = await this.simpleInstanceServiceProxy.InitInstance(
                new InitInstanceRequest
                {
                    Id = id,
                    TypeName = t.FullName,
                    AssemblyName = t.Assembly.GetName().FullName
                });

            if (!initResult.IsSuccess)
            { 
                throw new WorkerInstanceInitializeException(initResult.ExceptionMessage, initResult.FullExceptionString);  
            }

            return new CoreInstanceHandle(async () => await OnDispose(id));
        }

        private async Task OnDispose(long id)
        {
            var result = await this.simpleInstanceServiceProxy.DisposeInstance(
                            new DisposeInstanceRequest() { InstanceId = id });
            if (result.IsSuccess)
            {
                return;
            }

            throw new WorkerInstanceDisposeException(result.ExceptionMessage, result.FullExceptionString);
        }
    }
}
