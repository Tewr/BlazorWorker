var config = {};
var Module = {};
config.file_list = [];

// get blazor boot json for file list;
fetch('_framework/blazor.boot.json', {
    method: 'GET',
    headers: {
        'Content-Type': 'application/json'
    }
}).then(function (promise) {
    promise.json().then(function (response) {
        Module = {
            onRuntimeInitialized: function () {
                MONO.mono_load_runtime_and_bcl(
                    'app', //config.vfs_prefix,
                    '_framework/bin', //config.deploy_prefix,
                    true, //config.enable_debugging
                    response.assemblyReferences,
                    function () {
                        const messageHandler = Module.mono_bind_static_method("[BlazorWorker] Worker.Instance.MessageHandler");
                        self.onmessage = msg => {
                            messageHandler(msg.data);
                        };
                    }
                );
            }
        };

        self.importScripts('_framework/wasm/mono.js');
    });
}, function (error) {
    console.log(error);
});

