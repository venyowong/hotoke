using System;
using System.Configuration;
using ExtCore.Infrastructure.Actions;
using Hotoke.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Hotoke.GenericSearch
{
    public class AddGenericSearchAction : IConfigureServicesAction
    {
        public int Priority => 100;

        public void Execute(IServiceCollection serviceCollection, IServiceProvider serviceProvider)
        {
            var genericEngines = ConfigurationManager.AppSettings["genericengines"];
            if(string.IsNullOrWhiteSpace(genericEngines))
            {
                Console.WriteLine("Hotoke.GenericSearch.AddGenericSearchAction: Cannot get genericengines config from ConfigurationManager.AppSettings");
                return;
            }
            else
            {
                Console.WriteLine($"Hotoke.GenericSearch.AddGenericSearchAction: genericengines: {genericEngines}");
            }

            foreach(var engine in genericEngines.Split(',', ';'))
            {
                serviceCollection.AddSingleton<ISearchEngine>(new GenericSearch(engine));
                Console.WriteLine($"Hotoke.GenericSearch.AddGenericSearchAction: GenericSearch for {engine} has been injected");
            }
        }
    }
}
