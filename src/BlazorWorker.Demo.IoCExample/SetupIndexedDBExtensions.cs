using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using TG.Blazor.IndexedDB;

namespace BlazorWorker.Demo.IoCExample
{
    public static class SetupIndexedDBExtensions
    {
        public static IServiceCollection AddIndexedDbDemoPersonConfig(this IServiceCollection services)
        {
            services.AddIndexedDB(dbStore =>
            {
                dbStore.DbName = "TheFactory"; //example name
                dbStore.Version = 1;

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = "TestPersons",
                    PrimaryKey = new IndexSpec { Name = "id", KeyPath = "id", Auto = true },
                    Indexes = new List<IndexSpec>
                    {
                        new IndexSpec{ Name = "name", KeyPath = "name", Auto = false },
                    }
                });
            });

            return services;
        }
    }

    public class Person
    {
        public long? Id { get; set; }
        public string Name { get; set; }

    }
}
