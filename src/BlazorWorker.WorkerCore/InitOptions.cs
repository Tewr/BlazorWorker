using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWorker.Core
{
    public class WorkerInitOptions
    {
        public WorkerInitOptions()
        {
            FetchOverride = new Dictionary<string, FetchResponse>();
            DependentAssemblyFilenames = new string[] { };
            FetchUrlOverride = new Dictionary<string, string>();
        }

        public Dictionary<string, FetchResponse> FetchOverride { get; set; }
        public string[] DependentAssemblyFilenames { get; set; }
        public Dictionary<string, string> FetchUrlOverride { get; set; }
        public string MessageEndPoint { get; set; }
        public string InitEndPoint { get; set; }
        public string CallbackMethod { get; set; }

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {
            var redirects = new Dictionary<string, string>(this.FetchUrlOverride);
            foreach (var item in initOptions.FetchUrlOverride)
            {
                redirects[item.Key] = item.Value;
            }

            var fethOverride = new Dictionary<string, FetchResponse>(this.FetchOverride);
            foreach (var item in initOptions.FetchOverride)
            {
                fethOverride[item.Key] = item.Value;
            }

            return new WorkerInitOptions
            {
                DependentAssemblyFilenames = this.DependentAssemblyFilenames
                    .Concat(initOptions.DependentAssemblyFilenames)
                    .Distinct()
                    .ToArray(),
                FetchUrlOverride = redirects,
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                MessageEndPoint = initOptions.MessageEndPoint ?? this.MessageEndPoint,
                InitEndPoint = initOptions.InitEndPoint ?? this.InitEndPoint,
                FetchOverride = fethOverride
            };
        }
    }
}