using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonoWorker.Core.SimpleInstanceService
{
    public class SimpleInstanceService
    {
        public static readonly SimpleInstanceService Instance = new SimpleInstanceService();
        public readonly Dictionary<long, InstanceWrapper> instances = new Dictionary<long, InstanceWrapper>();

        public static readonly string MessagePrefix = $"{typeof(SimpleInstanceService).FullName}::";
        public static readonly string InitServiceResultMessagePrefix = $"{nameof(InitServiceResult)}::";
        public static readonly string InitInstanceMessagePrefix = $"{nameof(InitInstance)}::";
        public static readonly string InitInstanceResultMessagePrefix = $"{nameof(InitInstanceResult)}::";
        public static readonly string DiposeMessagePrefix = $"{nameof(DisposeInstance)}::";
        public static readonly string DiposeResultMessagePrefix = $"{nameof(DisposeResult)}::";

        public static void Init()
        {
            Instance.InnerInit();
        }

        private void InnerInit()
        {
            MessageService.Message += OnMessage;
            MessageService.PostMessage(new InitServiceResult().Serialize());
        }

        private void OnMessage(object sender, string rawMessage)
        {
            if (rawMessage.StartsWith(MessagePrefix) == false)
            {
                return;
            }

            if (InitInstanceRequest.CanDeserialize(rawMessage)) 
            {
                InitInstance(rawMessage);
                return;
            }

            if (DisposeInstanceRequest.CanDeserialize(rawMessage))
            {
                DisposeInstance(rawMessage);
                return;
            }
        }

        public void InitInstance(string initMessage)
        {
            var result = InitInstance(InitInstanceRequest.Deserialize(initMessage));
            MessageService.PostMessage(result.Serialize());
        }

        public void DisposeInstance(string message)
        {
            var result = DisposeInstance(DisposeInstanceRequest.Deserialize(message));
            MessageService.PostMessage(result.Serialize());
        }

        public InitInstanceResult InitInstance(InitInstanceRequest initInstanceRequest, 
            IsInfrastructureMessage handler = null)
        {
            var InstanceWrapper = new InstanceWrapper();
            var result = InitInstance(
                initInstanceRequest.CallId, 
                initInstanceRequest.TypeName, 
                initInstanceRequest.AssemblyName,
                () => (IWorkerMessageService)(InstanceWrapper.Services = new InjectableMessageService(IsInfrastructureMessage(handler))));

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

        private static IsInfrastructureMessage IsInfrastructureMessage(IsInfrastructureMessage handler)
        {
            return message => message.StartsWith(MessagePrefix) ||
                                (handler?.Invoke(message)).GetValueOrDefault(false);
        }

        public DisposeResult DisposeInstance(DisposeInstanceRequest request)
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

        private static InitInstanceResult InitInstance(long callId, string typeName, string assemblyName, Func<IWorkerMessageService> workerMessageServiceFactory)
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
                    CallId = callId,
                    Instance = instance,
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                return new InitInstanceResult
                {
                    CallId = callId,
                    ExceptionMessage = e.Message,
                    FullExceptionString = e.ToString(),
                    Exception = e,
                    IsSuccess = false
                };
            }
        }
    }
}
