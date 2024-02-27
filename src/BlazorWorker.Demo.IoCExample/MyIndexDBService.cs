using Microsoft.JSInterop;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TG.Blazor.IndexedDB;

namespace BlazorWorker.Demo.IoCExample
{
    public class MyIndexDBService
    {
        private readonly IJSRuntime jsRuntime;
        private readonly IndexedDBManager indexedDBManager;
        private bool isIndexDBManagerInitialized;

        public MyIndexDBService(
            IJSRuntime jsRuntime,
            IndexedDBManager indexDBManager)
        {
            //Console.WriteLine($"{nameof(MyIndexDBService)} instance created");
            this.jsRuntime = jsRuntime;
            this.indexedDBManager = indexDBManager;
        }

        public async Task<string> GetPersonName(long id)
        {
            try
            {
                //Console.WriteLine($"{nameof(GetPersonName)}({id}) called.");
                await EnsureInitializedAsync();
                //Console.WriteLine($"{nameof(GetPersonName)}({id}): Get Store name...");
                var storeName = indexedDBManager.Stores[0].Name;
                var testPersons = (await this.indexedDBManager.GetRecords<Person>(storeName));
                foreach (var item in testPersons)
                {
                    if (item.Id == id)
                    {
                        return item.Name ?? "[empty name]";
                    }
                }

                return "N/A";

            }
            catch (System.Exception e)
            {
                Console.Error.WriteLine($"{nameof(GetPersonName)} :{e}");
                throw;
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (!isIndexDBManagerInitialized)
            {
                // The following is a workaround as indexedDb.Blazor.js explicitly references "window"
                await this.jsRuntime.InvokeVoidAsync("eval", "(() => { self.window = self; return null; })()");
                await this.jsRuntime.InvokeVoidAsync("importLocalScripts", "_content/TG.Blazor.IndexedDB/indexedDb.Blazor.js");
                
                isIndexDBManagerInitialized = true;
            }
        }

    }
}
