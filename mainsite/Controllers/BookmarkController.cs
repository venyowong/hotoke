using System;
using System.Collections.Generic;
using System.Configuration;
using Hotoke.Common;
using Hotoke.MainSite;
using Hotoke.MainSite.Attributes;
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
        public bool Upsert(string url)
        {
            var data = Utility.GenerateHtmlDic(url);
            if(data == null)
            {
                return false;
            }

            try
            {
                HttpUtility.Post($"{this.appSettings.SearchHost}/{this.HttpContext.Items["user_id"]}/index", data);
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

            return new HotokeSearch($"{this.appSettings.SearchHost}/{this.HttpContext.Items["user_id"]}")
                .Search(keyword);
        }
    }
}