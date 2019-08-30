using System;
using System.Configuration;
using ExtCore.Infrastructure.Actions;
using Hotoke.Common;
using Microsoft.Extensions.DependencyInjection;
using Niolog;

namespace Hotoke.GenericSearch
{
    public class AddGenericSearchAction : IConfigureServicesAction
    {
        public int Priority => 100;

        public void Execute(IServiceCollection serviceCollection, IServiceProvider serviceProvider)
        {
            var logger = NiologManager.CreateLogger();
            var genericEngines = ConfigurationManager.AppSettings["genericengines"];
            if(string.IsNullOrWhiteSpace(genericEngines))
            {
                logger.Error()
                    .Message("Cannot get genericengines config from ConfigurationManager.AppSettings")
                    .Write();
                return;
            }
            else
            {
                logger.Info()
                    .Message($"genericengines: {genericEngines}")
                    .Write();
            }

            foreach(var engine in genericEngines.Split(',', ';'))
            {
                serviceCollection.AddSingleton<ISearchEngine>(new GenericSearch(engine));
                logger.Info()
                    .Message($"GenericSearch for {engine} has been injected")
                    .Write();
            }
        }
    }
}
