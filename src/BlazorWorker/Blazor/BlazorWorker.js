window.BlazorWorker = function () {
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker && worker.terminate) {
            worker.terminate();
            delete workers[workerId];
        }
    };

    const initWorker = function (id, callbackInstance) {

        // Initialize worker
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([window.BlazorWorker.scripts.Worker], { type: 'application/javascript' });
        const worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;
        // Setup callback message 
        const callBack = Blazor.platform.findMethod("BlazorWorker", "BlazorWorker.Blazor", "WebWorkerProxy", "OnMessage");

        worker.onmessage = function (ev) {
            Blazor.platform.callMethod(callBack, callbackInstance, id, ev.data);
        };
    };

    const postMessage = function (workerId, message) {
        workers[workerId].postMessage(message);
    };

    // Preserve this field set by init methods
    const scripts = window.BlazorWorker.scripts;

    return {
        disposeWorker,
        initWorker,
        postMessage,
        scripts 
    };
}();