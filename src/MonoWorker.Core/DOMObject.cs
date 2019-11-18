using System;
using WebAssembly;

namespace MonoWorker.Core
{
    // Serves as a wrapper around a JSObject.
    class DOMObject : IDisposable
    {
        public JSObject ManagedJSObject { get; private set; }

        public DOMObject(object jsobject)
        {
            ManagedJSObject = jsobject as JSObject;
            if (ManagedJSObject == null)
                throw new NullReferenceException($"{nameof(jsobject)} must be of type JSObject and non null!");

        }

        public DOMObject(string globalName) : this((JSObject)Runtime.GetGlobalObject(globalName))
        { }

        public object GetProperty(string property)
        {
            return ManagedJSObject.GetObjectProperty(property);
        }

        public object Invoke(string method, params object[] args)
        {
            return ManagedJSObject.Invoke(method, args);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {

            if (disposing)
            {

                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            ManagedJSObject?.Dispose();
            ManagedJSObject = null;
        }

    }
}
