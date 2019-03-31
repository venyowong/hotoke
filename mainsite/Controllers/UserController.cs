using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using Hotoke.MainSite.Entities;
using System;
using Hotoke.MainSite.Models;
using Hotoke.Common;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Hotoke.MainSite.Daos;
using Hotoke.MainSite.Attributes;

namespace Hotoke.MainSite.Controllers
{
    public class UserController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly ILogger<UserController> logger;

        public UserController(IOptions<AppSettings> appSettings, ILogger<UserController> logger)
        {
            this.appSettings = appSettings.Value;
            this.logger = logger;
        }

        [HttpPost]
        public ResultModel Login(string email, string password)
        {
            if(string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new ResultModel
                {
                    Success = false,
                    Message = "invalid parameters"
                };
            }

            using(var userDao = new UserDao(this.appSettings.Mysql?.ConnectionString))
            {
                var user = userDao.GetUserByEMail(email);
                if(user == null)
                {
                    var salt = Guid.NewGuid().ToString("N");
                    try
                    {
                        if(userDao.CreateUser(email, password) > 0 && 
                            (user = userDao.GetUserByEMail(email)) != null)
                        {
                            return new ResultModel<UserModel>
                            {
                                Success = true,
                                Result = this.PackUserModel(user)
                            };
                        }
                    }
                    catch(Exception e)
                    {
                        this.logger.LogError(e, "自动注册失败");
                    }
                    
                    return new ResultModel
                    {
                        Success = false,
                        Message = "failed to insert data into database"
                    };
                }
                else
                {
                    if(user.Password == $"{password}{user.Salt}".GetMd5Hash())
                    {
                        return new ResultModel<UserModel>
                        {
                            Success = true,
                            Result = this.PackUserModel(user)
                        };
                    }
                    else
                    {
                        return new ResultModel
                        {
                            Success = false,
                            Message = "wrong password"
                        };
                    }
                }
            }
        }

        [JwtAuthorize]
        public bool IsValidToken()
        {
            return true;
        }

        private UserModel PackUserModel(User user)
        {
            return new UserModel
            {
                Token = HttpUtility.Get(
                    $"{this.appSettings.Jwt?.Host}/token/generate?payload={JsonConvert.SerializeObject(new {user.EMail,user_id = user.Id})}"),
                EMail = user.EMail
            };
        }
    }
}