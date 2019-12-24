using System;

namespace MonoWorker.Core
{
    public class DisposeResult
    {
        public bool IsSuccess { get; set; }
        public long InstanceId { get; set; }

        public string ExceptionMessage { get; set; } = string.Empty;

        public string FullExceptionString { get; set; } = string.Empty;

        public Exception Exception { get; internal set; }
    }
}