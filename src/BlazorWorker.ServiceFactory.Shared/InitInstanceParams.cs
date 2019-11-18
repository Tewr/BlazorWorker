namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitInstanceParams
    {
        public string WorkerId { get; set; }
        public string InstanceId { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }
    }
}
