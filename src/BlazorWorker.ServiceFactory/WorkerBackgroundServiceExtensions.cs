using BlazorWorker.BackgroundServiceFactory.Shared;
using BlazorWorker.Core;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public static class WorkerBackgroundServiceExtensions
    {
        public static async Task<IWorkerBackgroundService<T>> CreateBackgroundServiceAsync<T>(this IWorker webWorkerProxy, WorkerInitOptions workerInitOptions = null) where T : class
        {
            var proxy = new WorkerBackgroundServiceProxy<T>(webWorkerProxy, new WebWorkerOptions());
            if (workerInitOptions == null)
            {
                workerInitOptions = new WorkerInitOptions()
                {
                    // Takes a (not so) wild guess and sets the dll name to the assembly name
                    DependentAssemblyFilenames = new[] { $"{typeof(T).Assembly.GetName().Name}.dll" }
                };
            }
            await proxy.InitAsync(workerInitOptions);
            return proxy;
        }

        public static string[] GuessReferencedDlls(this System.Type source)
        {
            var assembly = source.Assembly;
            return GuessReferencedDlls(assembly, new List<AssemblyName>()).ToArray();
        }

        private static IEnumerable<string> GuessReferencedDlls(Assembly forAssembly, List<AssemblyName> alreadyCovered)
        {
            yield return $"{forAssembly.GetName().Name}.dll";

            foreach (var assembly in forAssembly.GetReferencedAssemblies())
            {
                if (alreadyCovered.Contains(assembly))
                {
                    continue;
                }
                alreadyCovered.Add(assembly);

                yield return $"{assembly.Name}.dll";
                
                // recursion
                foreach (var childDep in GuessReferencedDlls(Assembly.ReflectionOnlyLoad(assembly.FullName), alreadyCovered))
                {
                    yield return childDep;
                }
            }
        }
    }
}

