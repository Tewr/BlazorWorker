using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class EventHandle
    {
        private static long idSource;
        public EventHandle()
        {
            Id = ++idSource;
        }
        public long Id { get; }

        public Action<object> EventHandler { get; set; }
    }
}
