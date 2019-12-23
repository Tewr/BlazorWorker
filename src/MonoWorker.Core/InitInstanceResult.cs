using System;

namespace MonoWorker.Core
{
    public class InitInstanceResult
    {
        public bool IsSuccess { get; set; }

        public object Instance { get; set; }

        public string ExceptionMessage { get; set; } = string.Empty;

        public string FullExceptionString { get; set; } = string.Empty;

        public Exception Exception { get; internal set; }
    }
}