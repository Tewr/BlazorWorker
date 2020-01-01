using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonoWorker.Core.SimpleInstanceService
{
    public class SimpleInstanceService : ISimpleInstanceService
    {
        public static readonly SimpleInstanceService Instance = new SimpleInstanceService();
        public readonly Dictionary<long, InstanceWrapper> instances = new Dictionary<long, InstanceWrapper>();

        public static readonly string MessagePrefix = $"{typeof(SimpleInstanceService).FullName}::";
        public static readonly string InitMessagePrefix = $"{nameof(InitInstance)}::";
        public static readonly string InitResultMessagePrefix = $"{nameof(InitInstanceResult)}::";
        public static readonly string DiposeMessagePrefix = $"{nameof(DisposeInstance)}::";
        public static readonly string DiposeResultMessagePrefix = $"{nameof(DisposeResult)}::";

        public static void Init()
        {
            Instance.InnerInit();
        }

        private void InnerInit()
        {
            MessageService.Message += OnMessage;
        }

        private void OnMessage(object sender, string rawMessage)
        {
            if (rawMessage.StartsWith(MessagePrefix) == false)
            {
                return;
            }

            if (InitInstanceRequest.CanDeserialize(rawMessage)) 
            {
                InitInstance(InitInstanceRequest.Deserialize(rawMessage));
                return;
            }

            if (DisposeInstanceRequest.CanDeserialize(rawMessage))
            {
                DisposeInstance(DisposeInstanceRequest.Deserialize(rawMessage));
                return;
            }
        }

        public async Task InitInstance(string initMessage)
        {
            var result = await InitInstance(InitInstanceRequest.Deserialize(initMessage));
            MessageService.PostMessage(result.Serialize());
        }

        public async Task DisposeInstance(string message)
        {
            var result = await DisposeInstance(DisposeInstanceRequest.Deserialize(message));
            MessageService.PostMessage(result.Serialize());
        }

        public async Task<InitInstanceResult> InitInstance(InitInstanceRequest initInstanceRequest)
        {
            return InitInstance(initInstanceRequest, null);
        }

        public InitInstanceResult InitInstance(InitInstanceRequest initInstanceRequest, 
            IsInfrastructureMessage handler = null)
        {
            var InstanceWrapper = new InstanceWrapper();
            var result = InitInstance(initInstanceRequest.TypeName, initInstanceRequest.AssemblyName,
                () => (IWorkerMessageService)(InstanceWrapper.Services 
                    = new InjectableMessageService(message => (handler?.Invoke(message)).GetValueOrDefault(false))));

            InstanceWrapper.Instance = result.Instance;
            if (result.IsSuccess)
            {
                instances[initInstanceRequest.Id] = InstanceWrapper;
            }
            else
            {
                InstanceWrapper.Dispose();
            }

            return result;
        }




        public async Task<DisposeResult> DisposeInstance(DisposeInstanceRequest request)
        {
            if (!instances.TryGetValue(request.InstanceId, out var instanceWrapper)) {
                return new DisposeResult
                {
                    CallId = request.CallId,
                    InstanceId = request.InstanceId,
                    IsSuccess = false
                };
            }

            try
            {
                instanceWrapper.Dispose();

                instances.Remove(request.InstanceId);
                return new DisposeResult { 
                    InstanceId = request.InstanceId,
                    CallId = request.CallId,
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                return new DisposeResult
                {
                    CallId = request.CallId,
                    InstanceId = request.InstanceId,
                    IsSuccess = false,
                    Exception = e,
                    ExceptionMessage = e.Message,
                    FullExceptionString = e.ToString()
                };
            }
        }

        private static InitInstanceResult InitInstance(string typeName, string assemblyName, Func<IWorkerMessageService> workerMessageServiceFactory)
        {
            try
            {
                var type = Type.GetType($"{typeName}, {assemblyName}", true);
                var constructors = type.GetConstructors();
                ConstructorInfo constructorInfo;
                var lastMatchArgCount = -1;
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Length == 0 && lastMatchArgCount < 0)
                    {
                        lastMatchArgCount = 0;
                        constructorInfo = constructor;
                        continue;
                    }

                    if (parameters.Length == 1 && lastMatchArgCount < 1)
                    {
                        if (parameters[0].ParameterType == typeof(IWorkerMessageService))
                        {
                            lastMatchArgCount = 1;
                            constructorInfo = constructor;
                            continue;
                        }
                    }
                }

                object instance;

                if (lastMatchArgCount == 0)
                {
                    instance = Activator.CreateInstance(type);
                }
                else if (lastMatchArgCount == 1)
                {
                    instance = Activator.CreateInstance(type, workerMessageServiceFactory());
                }
                else {
                    throw new InvalidOperationException($"Unable to find compatible constructor for activating type '{type}'.");
                }

                return new InitInstanceResult()
                {
                    Instance = instance,
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                return new InitInstanceResult
                {
                    ExceptionMessage = e.Message,
                    FullExceptionString = e.ToString(),
                    Exception = e,
                    IsSuccess = false
                };
            }
        }
    }
}
