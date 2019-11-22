using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BlazorWorker.Core
{
    public class WorkerInitOptions
    {
        public WorkerInitOptions()
        {
            staticAssemblyRefs = new string[] { };
            assemblyRedirectByFilename = new Dictionary<string, string>();
        }

        public string[] staticAssemblyRefs { get; set; }
        public Dictionary<string, string> assemblyRedirectByFilename { get; set; }
        public string messageEndPoint { get; set; }
        public string initEndPoint { get; set; }
        public string callbackMethod { get; internal set; }

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {
            var redirects = new Dictionary<string, string>(this.assemblyRedirectByFilename);
            foreach (var item in initOptions.assemblyRedirectByFilename)
            {
                redirects[item.Key] = item.Value;
            }

            return new WorkerInitOptions
            {
                staticAssemblyRefs = this.staticAssemblyRefs
                    .Concat(initOptions.staticAssemblyRefs)
                    .Distinct()
                    .ToArray(),
                assemblyRedirectByFilename = redirects,
                callbackMethod = initOptions.callbackMethod ?? this.callbackMethod,
                messageEndPoint = initOptions.messageEndPoint ?? this.messageEndPoint,
                initEndPoint = initOptions.initEndPoint ?? this.initEndPoint,
            };
        }
    }
}