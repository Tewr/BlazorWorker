window.BlazorWorker = function () {
    
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker && worker.terminate) {
            worker.terminate();
            delete workers[workerId];
        }
    };

    const workerDef = function () {
        const onReady = () => {
            console.debug("mono loaded.");
            const messageHandler =
                Module.mono_bind_static_method(initConf.MessageEndPoint);
            // Future messages goes directly to the message handler
            self.onmessage = msg => {
                messageHandler(msg.data);
            };

            if (!initConf.InitEndPoint) {
                return;
            }

            try {
                Module.mono_call_static_method(initConf.InitEndPoint, []);
            } catch (e) {
                console.error(`Init method ${initConf.InitEndPoint} failed`, e);
                throw e;
            }
        };

        const onError = (err) => {
            console.error(err);
        };

        function asyncLoad(url) {
            return new Promise((resolve, reject) => {
                const xhr = new XMLHttpRequest();
                xhr.open('GET', url, /* async: */ true);
                xhr.responseType = 'arraybuffer';
                xhr.onload = function xhr_onload() {
                    if (xhr.status == 200 || xhr.status == 0 && xhr.response) {
                        const asm = new Uint8Array(xhr.response);
                        resolve(asm);
                    } else {
                        reject(xhr);
                    }
                };
                xhr.onerror = reject;
                xhr.send(undefined);
            });
        }
        const initConf = JSON.parse('$initConf$');
        var config = {};
        var Module = {};
        
        const wasmBinaryFile = `${initConf.appRoot}/_framework/wasm/dotnet.wasm`;
        const suppressMessages = ['DEBUGGING ENABLED'];
        const appBinDirName = 'appBinDir';

        Module.print = line => (suppressMessages.indexOf(line) < 0 && console.log(`WASM: ${line}`));

        Module.printErr = line => {
            console.error(`WASM: ${line}`);
            showErrorNotification();
        };
        Module.preRun = [];
        Module.postRun = [];
        Module.preloadPlugins = [];

        Module.locateFile = fileName => {
            switch (fileName) {
                case 'dotnet.wasm': return wasmBinaryFile;
                default: return fileName;
            }
        };

        Module.preRun.push(() => {
            // By now, emscripten should be initialised enough that we can capture these methods for later use
            const mono_wasm_add_assembly = Module.cwrap('mono_wasm_add_assembly', null, [
                'string',
                'number',
                'number',
            ]);

            mono_string_get_utf8 = Module.cwrap('mono_wasm_string_get_utf8', 'number', ['number']);

            MONO.loaded_files = [];
            var baseUrl = `${initConf.appRoot}/${initConf.deploy_prefix}`;
            initConf.DependentAssemblyFilenames.forEach(url => {
                
                const runDependencyId = `blazor:${url}`;
                addRunDependency(runDependencyId);
                asyncLoad(baseUrl+'/'+ url).then(
                    data => {
                        const heapAddress = Module._malloc(data.length);
                        const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress, data.length);
                        heapMemory.set(data);
                        mono_wasm_add_assembly(url, heapAddress, data.length);
                        MONO.loaded_files.push(url);
                        removeRunDependency(runDependencyId);
                    },
                    errorInfo => {
                        // If it's a 404 on a .pdb, we don't want to block the app from starting up.
                        // We'll just skip that file and continue (though the 404 is logged in the console).
                        // This happens if you build a Debug build but then run in Production environment.
                        const isPdb404 = errorInfo instanceof XMLHttpRequest
                            && errorInfo.status === 404
                            && filename.match(/\.pdb$/);
                        if (!isPdb404) {
                            onError(errorInfo);
                        }
                        removeRunDependency(runDependencyId);
                    }
                );
            });
        });

        Module.postRun.push(() => {
            MONO.mono_wasm_setenv("MONO_URI_DOTNETRELATIVEORABSOLUTE", "true");
            const load_runtime = Module.cwrap('mono_wasm_load_runtime', null, ['string', 'number']);
            load_runtime(appBinDirName, 0);
            MONO.mono_wasm_runtime_is_ready = true;
            //attachInteropInvoker();
            onReady();
        });

        config.file_list = [];
        
        global = globalThis;
        self.Module = Module;

        self.importScripts(`${initConf.appRoot}/${initConf.wasmRoot}/dotnet.js`); 
    };

    const inlineWorker = `self.onmessage = ${workerDef}()`; 

    const initWorker = function (id, callbackInstance, initOptions) {
        let appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substr(0, appRoot.length - 1);
        }

        const initConf = {
            appRoot: appRoot,
            DependentAssemblyFilenames: initOptions.dependentAssemblyFilenames,
            //FetchUrlOverride: initOptions.fetchUrlOverride,
            deploy_prefix: "_framework/_bin",
            MessageEndPoint: initOptions.messageEndPoint,
            InitEndPoint: initOptions.initEndPoint,
            wasmRoot: "_framework/wasm",
            //FetchOverride: initOptions.fetchOverride,
            debug: initOptions.debug
        };
        // Initialize worker
        const renderedConfig = JSON.stringify(initConf).replace('$appRoot$', appRoot);
        const renderedInlineWorker = inlineWorker.replace('$initConf$', renderedConfig);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;

        worker.onmessage = function (ev) {
            if (initOptions.debug) {
                console.debug(`BlazorWorker.js:worker->blazor`, initOptions.callbackMethod, ev.data);
            }
            callbackInstance.invokeMethod(initOptions.callbackMethod, ev.data);
        };
    };

    const postMessage = function (workerId, message) {
        workers[workerId].postMessage(message);
    };

    return {
        disposeWorker,
        initWorker,
        postMessage
    };
}();