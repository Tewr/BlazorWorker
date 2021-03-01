﻿window.BlazorWorker = function () {
    
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker && worker.terminate) {
            worker.terminate();
            delete workers[workerId];
        }
    };

    const workerDef = function () {
        const initConf = JSON.parse('$initConf$');
        const nonExistingDlls = [];
        let blazorBootManifest = {
            resources: { assembly: { "AssemblyName.dll": "sha256-<sha256>" } }
        };

        let endInvokeCallBack;
        const onReady = () => {

            endInvokeCallBack =
                Module.mono_bind_static_method(initConf.endInvokeCallBackEndpoint);
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

        function asyncLoad(url, reponseType) {
            return new Promise((resolve, reject) => {
                const xhr = new XMLHttpRequest();
                const arrayBufferType = 'arraybuffer';
                xhr.open('GET', url, /* async: */ true);
                xhr.responseType = reponseType || arrayBufferType;
                xhr.onload = function xhr_onload() {
                    if (xhr.status == 200 || xhr.status == 0 && xhr.response) {
                        if (this.responseType === arrayBufferType) {
                            const asm = new Uint8Array(xhr.response);
                            resolve(asm);
                        } else {
                            resolve(xhr.response);
                        }
                    } else {
                        reject(xhr);
                    }
                };
                xhr.onerror = reject;
                xhr.send(undefined);
            });
        }
        
        var config = {};
        var Module = {};
        
        const wasmBinaryFile = `${initConf.appRoot}/${initConf.wasmRoot}/dotnet.wasm`;
        const suppressMessages = ['DEBUGGING ENABLED'];
        const appBinDirName = 'appBinDir';

        Module.print = line => (suppressMessages.indexOf(line) < 0 && console.log(`WASM-WORKER: ${line}`));

        Module.printErr = line => {
            console.error(`WASM-WORKER: ${line}`);
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
            const mono_wasm_add_assembly = Module.cwrap('mono_wasm_add_assembly', null, [
                'string',
                'number',
                'number',
            ]);

            mono_string_get_utf8 = Module.cwrap('mono_wasm_string_get_utf8', 'number', ['number']);

            MONO.loaded_files = [];
            var baseUrl = `${initConf.appRoot}/${initConf.deploy_prefix}`;
            
            initConf.DependentAssemblyFilenames.forEach(url => {

                if (!blazorBootManifest.resources.assembly.hasOwnProperty(url)) {
                    //Do not attempt to load a dll which is not present anyway
                    nonExistingDlls.push(url);
                    return;
                }

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
                        const isPdb404 = errorInfo instanceof XMLHttpRequest
                            && errorInfo.status === 404
                            && url.match(/\.pdb$/);
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
            onReady();
            if (initConf.debug && nonExistingDlls.length > 0) {
                console.warn(`BlazorWorker: Module.postRun: ${nonExistingDlls.length} assemblies was specified as a dependency for the worker but was not present in the bootloader. This may be normal if trimmming is used. To remove this warning, either configure the linker not to trim the specified assemblies if they were removed in error, or conditionally remove the specified dependencies for builds that uses trimming. If trimming is not used, make sure that the assembly is included in the build.`, nonExistingDlls);
            }
        });

        config.file_list = [];
        
        global = globalThis;
        self.Module = Module;

        //TODO: This call could/should be session cached. But will the built-in blazor fetch service worker override 
        // (PWA et al) do this already if configured ?
        asyncLoad(`${initConf.appRoot}/${initConf.blazorBoot}`, 'json')
            .then(blazorboot => {
                blazorBootManifest = blazorboot;
                let dotnetjsfilename = '';
                const runttimeSection = blazorboot.resources.runtime;
                for (var p in runttimeSection) {
                    if (Object.prototype.hasOwnProperty.call(runttimeSection, p) && p.endsWith('.js')) {
                        dotnetjsfilename = p;
                    }
                }

                if (dotnetjsfilename === '') {
                    throw 'BlazorWorker: Unable to locate dotnetjs file in blazor boot config.';
                }

                self.importScripts(`${initConf.appRoot}/${initConf.wasmRoot}/${dotnetjsfilename}`);
            
            }, errorInfo => onError(errorInfo));

        self.jsRuntimeSerializers = new Map();
        self.jsRuntimeSerializers.set('nativejson', {
            serialize: o => JSON.stringify(o),
            deserialize: s => JSON.parse(s)
        });
            
        // Async invocation with callback.
        self.beginInvokeAsync = async function (serializerId, invokeId, method, argsString) {

            let result;
            let isError = false;
            var serializer = self.jsRuntimeSerializers.get(serializerId);
            if (!serializer) {
                result = `beginInvokeAsync: Unknown serializer with id '${serializerId}'`;
                isError = true;
            }

            if (!isError && !Object.hasOwnProperty.call(self, method)) {
                const error = `beginInvokeAsync: Method '${method}' not defined`;
                isError = true;
            }

            if (!isError) {

                const methodHandle = self[method];

                try {
                    const argsArray = serializer.deserialize(argsString);
                    result = await methodHandle(...argsArray);
                }
                catch (e) {
                    result = `${e}\nJS Stacktrace:${new Error().stack}`;
                    isError = true;
                }
            }
            
            try {
                const resultString = serializer.serialize(result);
                endInvokeCallBack(invokeId, isError, resultString);
            } catch (e) {
                console.error(`BlazorWorker: beginInvokeAsync: Callback to ${initConf.endInvokeAsyncCallBackEndpoint} failed`, e);
                throw e;
            }
        };
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
            deploy_prefix: initOptions.deployPrefix,
            MessageEndPoint: initOptions.messageEndPoint,
            InitEndPoint: initOptions.initEndPoint,
            endInvokeCallBackEndpoint: initOptions.endInvokeCallBackEndpoint,
            wasmRoot: initOptions.wasmRoot,
            blazorBoot: "_framework/blazor.boot.json",
            debug: initOptions.debug
        };

        // Initialize worker
        const renderedConfig = JSON.stringify(initConf);
        const renderedInlineWorker = inlineWorker.replace('$initConf$', renderedConfig);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;

        worker.onmessage = function (ev) {
            if (initOptions.debug) {
                console.debug(`BlazorWorker.js:worker[${id}]->blazor`, initOptions.callbackMethod, ev.data);
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