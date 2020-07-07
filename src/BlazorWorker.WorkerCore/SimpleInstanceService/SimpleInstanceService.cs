using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
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

        static SimpleInstanceService()
        {
            AppDomain.CurrentDomain.AssemblyResolve += LogFailedAssemblyResolve;
        }

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

        private class ServiceCollection: Dictionary<Type, Func<object>>
        {
            public void Add<T>(Func<T> factory)
            {
                this.Add(typeof(T), () => factory());
            }
        }

        private static InitInstanceResult InitInstance(long callId, string typeName, string assemblyName, Func<IWorkerMessageService> workerMessageServiceFactory)
        {
            var services = new ServiceCollection
            {
                () => new HttpClient(),
                workerMessageServiceFactory
            };

            try
            {
                var type = Type.GetType($"{typeName}, {assemblyName}", true);
                var constructors = type.GetConstructors();
                ConstructorInfo constructorInfo = null;
                var lastMatchArgCount = -1;
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    if (lastMatchArgCount < parameters.Length)
                    {
                        if (parameters.All(parameter => services.ContainsKey(parameter.ParameterType)))
                        {
                            lastMatchArgCount = parameters.Length;
                            constructorInfo = constructor;
                        }
                    }
                }

                if (constructorInfo == null)
                {
                    throw new InvalidOperationException($"Unable to find compatible constructor for activating type '{type}'.");
                }

                // Create instances for each constructor argument matching a supported service.
                var serviceInstances = constructorInfo.GetParameters().Select(parameter => services[parameter.ParameterType].Invoke()).ToArray();
                
                var instance = constructorInfo.Invoke(serviceInstances);

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

        private static Assembly LogFailedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.Error.WriteLine($"'{args.RequestingAssembly}' is requesting missing assembly '{args.Name}')");

            // Nobody really cares about this exception for now, it can't be caught.
            throw new InvalidOperationException($"'{args.RequestingAssembly}' is requesting missing assembly '{args.Name}')");
        }
    }
}
