window.BlazorWorker = function () {
    const workers = {};
    const disposeWorker = function () {};
    const initWorker = function (id) {
        window.URL = window.URL || window.webkitURL;
        var blob = new Blob([response], { type: 'application/javascript' });
        var worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;
    };

    const initInstance = function () { };
    const methodCallVoid = function () { };
    const methodCall = function () { };

    // Preserve this field set by init methods
    const scripts = window.BlazorWorker.scripts;

    return {
        disposeWorker,
        initWorker,
        initInstance,
        methodCallVoid,
        methodCall,
        scripts 
    };
}();