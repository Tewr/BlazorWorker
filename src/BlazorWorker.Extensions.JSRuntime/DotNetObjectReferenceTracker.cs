using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BlazorWorker.Extensions.JSRuntime
{
    public class DotNetObjectReferenceTracker
    {
        public static JsonEncodedText DotNetObjectRefKey = JsonEncodedText.Encode("__dotNetObject");

        private static TrackerJsRuntime objectTracker = new TrackerJsRuntime();
        private static ConditionalWeakTable<object, BlazorWorkerJSRuntime> jsRuntimeReferences =
            new ConditionalWeakTable<object, BlazorWorkerJSRuntime>();

        internal static DotNetObjectReference<T> GetObjectReference<T>(long dotNetObjectId) where T : class
        {
            return objectTracker.InternalGetObjectReference<T>(dotNetObjectId);
        }

        internal static object GetObjectReferenceObject(long dotNetObjectId)
        {
            return objectTracker.InternalGetObjectReference<T>(dotNetObjectId);
        }

        internal static long TrackObjectReference<T>(DotNetObjectReference<T> value) where T: class
        {
            return objectTracker.InternalTrackObjectReference<T>(value);
        }

        public static void SetCallbackJSRuntime<T>(DotNetObjectReference<T> source, BlazorWorkerJSRuntime jsruntime) where T : class
        {
            jsRuntimeReferences.Add(source, jsruntime);
        }
        public static BlazorWorkerJSRuntime GetCallbackJSRuntime(object source)
        {
            return jsRuntimeReferences.GetValue(source, _ => (BlazorWorkerJSRuntime)null);
        }

        internal class TrackerJsRuntime : Microsoft.JSInterop.JSRuntime
        {
            private static readonly Type selfType = typeof(TrackerJsRuntime);

            public delegate long TrackObjectReferenceDelegate<T>(DotNetObjectReference<T> value) where T : class;
            public delegate DotNetObjectReference<T> GetObjectReferenceDelegate<T>(long dotNetObjectId) where T : class;

            private static readonly GenericNonPublicDelegateCache GetObjectReferenceDelegates = 
                new GenericNonPublicDelegateCache(selfType);
            private static readonly GenericNonPublicDelegateCache TrackObjectReferenceDelegates =
                new GenericNonPublicDelegateCache(selfType);


            public DotNetObjectReference<T> InternalGetObjectReference<T>(long dotNetObjectId) where T : class
            {
                var method = GetObjectReferenceDelegates
                    .GetDelegate<GetObjectReferenceDelegate<T>, T, long>
                        (this, nameof(DotNetObjectReferenceTracker.GetObjectReference));

                return method.Invoke(dotNetObjectId);
            }

            public long InternalTrackObjectReference<T>(DotNetObjectReference<T> value)  where T : class
            {
                var method = TrackObjectReferenceDelegates
                    .GetDelegate<TrackObjectReferenceDelegate<T>, T, DotNetObjectReference<T>>
                        (this, nameof(DotNetObjectReferenceTracker.TrackObjectReference));

                return method.Invoke(value);
            }



            #region Unsupported methods
            
            protected override void BeginInvokeJS(long taskId, string identifier, string argsJson)
            {
                throw new NotSupportedException();
            }

            protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            {
                throw new NotSupportedException();
            }

#if NET5
            protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
            {
                throw new NotSupportedException();
            }
#endif

            #endregion
        }
    }
}
