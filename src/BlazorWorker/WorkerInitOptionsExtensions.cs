namespace BlazorWorker.Core
{
    /// <summary>
    /// Contains convenience extensions for <see cref="WorkerInitOptions"/>
    /// </summary>
    public static class WorkerInitOptionsExtensions
    {
        /// <summary>
        /// Set the specified <paramref name="environmentVariableName"/> to the specified <paramref name="value"/> when the worker runtime has been initialized
        /// </summary>
        /// <param name="source"></param>
        /// <param name="environmentVariableName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public static WorkerInitOptions SetEnv(this WorkerInitOptions source, string environmentVariableName, string value)
        {
            source.EnvMap[environmentVariableName] = value;
            return source;
        }
    }
}