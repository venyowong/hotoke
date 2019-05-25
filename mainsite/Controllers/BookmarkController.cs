using System;
using System.Collections.Generic;
using System.Configuration;
using Hotoke.Common;
using Hotoke.MainSite;
using Hotoke.MainSite.Attributes;
using Hotoke.MainSite.Queries;
using Hotoke.SearchEngines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hotoke.MainSite.Controllers
{
    [JwtAuthorize]
    public class BookmarkController : Controller
    {
        private readonly AppSettings appSettings;

        public BookmarkController(IOptions<AppSettings> appSettings)
        {
            this.appSettings = appSettings.Value;
        }

        [HttpPost]
        public bool Upsert(string url, string path, string title)
        {
            var data = Utility.GenerateHtmlDic(url);
            if(data == null)
            {
                return false;
            }

            try
            {
                var userId = this.HttpContext.Items["user_id"].ToString();
                data.Add("user_id", userId);
                data.Add("path", path);
                data.Add("remark", title);
                HttpUtility.PostJson($"{this.appSettings.EsHost}/bookmark/bookmark/{userId}-{path.GetHashCode()}", data);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public IEnumerable<SearchResult> Search(string keyword)
        {
            if(string.IsNullOrWhiteSpace(keyword))
            {
                return null;
            }
            
            return new ElasticSearch().Search("bookmark", BookmarkQueryBuilder.BuildKeywordQuery(
                this.HttpContext.Items["user_id"].ToString(), keyword));
        }
    }
}