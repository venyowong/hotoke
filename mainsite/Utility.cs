using System.Collections.Generic;
using System.Configuration;
using Hotoke.Common;
using Hotoke.MainSite.Models;

namespace Hotoke.MainSite
{
    static class Utility
    {
        public static string[] BadUrls;

        static Utility()
        {
            try
            {
                BadUrls = ConfigurationManager.AppSettings["badurls"].Split(';');
            }
            finally
            {
                BadUrls = new string[0];
            }
        }

        public static SearchResultModel Copy(this SearchResultModel model)
        {
            if(model == null)
            {
                return null;
            }

            var newModel = new SearchResultModel
            {
                RequestId = model.RequestId,
                Searched = model.Searched,
                Finished = model.Finished
            };
            if(model.Results != null)
            {
                newModel.Results = new List<SearchResult>();
                newModel.Results.AddRange(model.Results);
            }
            return newModel;
        }
    }
}