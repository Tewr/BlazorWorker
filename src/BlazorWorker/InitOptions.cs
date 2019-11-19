using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BlazorWorker.Core
{
    public class InitOptions
    {
        public InitOptions()
        {
            staticAssemblyRefs = new string[] { };
            assemblyRedirectByFilename = new Dictionary<string, string>();
        }

        public string[] staticAssemblyRefs { get; set; }
        public Dictionary<string, string> assemblyRedirectByFilename { get; set; }
        public string messageEndPoint { get; set; }
        public string callbackMethod { get; internal set; }

        public InitOptions MergeWith(InitOptions initOptions)
        {
            var redirects = new Dictionary<string, string>(this.assemblyRedirectByFilename);
            foreach (var item in initOptions.assemblyRedirectByFilename)
            {
                redirects[item.Key] = item.Value;
            }

            return new InitOptions
            {
                staticAssemblyRefs = this.staticAssemblyRefs
                    .Concat(initOptions.staticAssemblyRefs)
                    .Distinct()
                    .ToArray(),
                assemblyRedirectByFilename = redirects,
                callbackMethod = initOptions.callbackMethod ?? this.callbackMethod,
                messageEndPoint = initOptions.messageEndPoint ?? this.messageEndPoint
            };
        }
    }
}