using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Hotoke.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Hotoke.MainSite.Attributes
{
    public class JwtAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Request.Headers.TryGetValue("access-token", out StringValues values);
            var token = values.FirstOrDefault();
            if(string.IsNullOrWhiteSpace(token))
            {
                context.Result = new StatusCodeResult(401);
                return;
            }

            var dic = HttpUtility.Get<Dictionary<object, object>>($"{ConfigurationManager.AppSettings["jwthost"]}/token/identity?token={token}");
            if(dic == null)
            {
                context.Result = new StatusCodeResult(401);
                return;
            }

            foreach(var pair in dic)
            {
                context.HttpContext.Items.TryAdd(pair.Key, pair.Value);
            }
        }
    }
}