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
                    '_framework', //config.vfs_prefix,
                    '_framework/bin', //config.deploy_prefix,
                    false, //config.enable_debugging
                    response.assemblyReferences,
                    function () {
                        App.init();
                    }
                );
            }
        };

        self.importScripts('_framework/wasm/mono.js');
        self.postMessage(data);
    });
}, function (error) {
    console.log(error);
});

