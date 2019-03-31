using System;
using System.Collections.Generic;
using System.Linq;
using Hotoke.Common;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StanSoft;

namespace Hotoke.MainSite.Controllers
{
    public class BehaviorController : Controller
    {
        private readonly ILogger<BehaviorController> logger;
        private readonly AppSettings appSettings;

        public BehaviorController(ILogger<BehaviorController> logger, IOptions<AppSettings> appSettings)
        {
            this.logger = logger;
            this.appSettings = appSettings.Value;
        }

        [HttpPost]
        public void Browse(string url)
        {
            var data = Utility.GenerateHtmlDic(url);
            if(data == null)
            {
                return;
            }

            HttpUtility.Post($"{this.appSettings.SearchHost}/index", data);
        }
    }
}