using System;
using Hotoke.Core.Searchers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hotoke.Core.AspNetCore
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddHotoke(this IServiceCollection services, string searcherName, 
            IConfiguration customSearcherConfig = null, Action<MetaSearcherConfig> metaSearcherConfigAction = null)
        {
            switch (searcherName?.ToLower())
            {
                case "weightfirst":
                    services.AddSingleton<BaseMetaSearcher, WeightFirstSearcher>();
                    break;
                case "custom":
                    services.AddSingleton<BaseMetaSearcher, CustomSearcher>();
                    break;
                default:
                    services.AddSingleton<BaseMetaSearcher, ParallelSearcher>();
                    break;
            }
            if (customSearcherConfig != null)
            {
                services.AddOptions()
                    .Configure<CustomSearcherConfig>(customSearcherConfig);
            }

            var metaSearcherConfig = new MetaSearcherConfig();
            if (metaSearcherConfigAction != null)
            {
                metaSearcherConfigAction(metaSearcherConfig);
            }
            services.AddSingleton<MetaSearcherConfig>(metaSearcherConfig);
            return services;
        }
    }
}