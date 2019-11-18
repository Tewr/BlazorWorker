self.onmessage = function (message) {

    var config = {
        vfs_prefix: "managed",
        deploy_prefix: "managed",
        enable_debugging: 0,
        file_list: [

            "BlazorWorker.Demo.Shared.dll",
            "BlazorWorker.dll",
            "Microsoft.AspNetCore.Blazor.dll",
            "Microsoft.AspNetCore.Blazor.HttpClient.dll",
            "Microsoft.AspNetCore.Components.dll",
            "Microsoft.AspNetCore.Components.Forms.dll",
            "Microsoft.AspNetCore.Components.Web.dll",
            "Microsoft.Bcl.AsyncInterfaces.dll",
            "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
            "Microsoft.Extensions.DependencyInjection.dll",
            "Microsoft.Extensions.Logging.Abstractions.dll",
            "Microsoft.Extensions.Options.dll",
            "Microsoft.Extensions.Primitives.dll",
            "Microsoft.JSInterop.dll",
            "Mono.Security.dll",
            "Mono.WebAssembly.Interop.dll",
            "netstandard.dll",
            "mscorlib.dll",
            "Newtonsoft.Json.dll",
            "Serialize.Linq.dll",
            "System.Buffers.dll",
            "System.ComponentModel.Annotations.dll",
            "System.Core.dll",
            "System.Data.dll",
            "System.dll",
            "System.Memory.dll",
            "System.Net.Http.dll",
            "System.Numerics.dll",
            "System.Numerics.Vectors.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
            "System.Runtime.Serialization.dll",
            "System.ServiceModel.Internals.dll",
            "System.Text.Encodings.Web.dll",
            "System.Text.Json.dll",
            "System.Threading.Tasks.Extensions.dll",
            "System.Xml.dll",
            "System.Xml.Linq.dll",
            "WebAssembly.Bindings.0.2.2.0.dll",
            "BlazorWorker.Demo.Client.pdb",
            "BlazorWorker.Demo.Shared.pdb",
            "BlazorWorker.pdb",
            "WebAssembly.Bindings.pdb"



            /*
            "sample.dll",
            "mscorlib.dll",
            "System.Net.Http.dll",
            "System.dll",
            "Mono.Security.dll",
            "System.Xml.dll",
            "System.Numerics.dll",
            "System.Core.dll",
            "WebAssembly.Net.Http.dll",
            "netstandard.dll",
            "System.Data.dll",
            "System.Transactions.dll",
            "System.Data.DataSetExtensions.dll",
            "System.Drawing.Common.dll",
            "System.IO.Compression.dll",
            "System.IO.Compression.FileSystem.dll",
            "System.ComponentModel.Composition.dll",
            "System.Runtime.Serialization.dll",
            "System.ServiceModel.Internals.dll",
            "System.Xml.Linq.dll",
            "WebAssembly.Bindings.dll",
            "System.Memory.dll",
            "WebAssembly.Net.WebSockets.dll"*/]
    };

    Module = self.Module = {
        onRuntimeInitialized: function () {
            MONO.mono_load_runtime_and_bcl(
                config.vfs_prefix,
                config.deploy_prefix,
                config.enable_debugging,
                config.file_list,
                function () {
                    console.debug("This should work.");
                    
                    const messageHandler = Module.mono_bind_static_method("[BlazorWorker]BlazorWorker.Worker.Worker:MessageHandler");
                    self.onmessage = msg => {
                        messageHandler(msg.data);
                    };
                    console.debug("Setup done!");
                    // Treat the first message *later*
                    //messageHandler(firstMessage.data);

                    self.postMessage("test callback from worker.js");
                }
            );
        }
    };

    self.importScripts(`/mono.js`);
};