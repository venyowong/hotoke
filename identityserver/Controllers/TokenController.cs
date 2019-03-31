using System;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace JwtIdentityServer.Controllers
{
    public class TokenController : Controller
    {
        private double expiration;
        private string secret;

        public TokenController(IConfiguration configuration)
        {
            double.TryParse(configuration["Jwt:Expiration"], out this.expiration);
            this.secret = configuration["Jwt:Secret"];

            if(this.expiration <= 0)
            {
                throw new Exception("invalid Jwt:Expiration");
            }
            if(string.IsNullOrWhiteSpace(this.secret))
            {
                throw new Exception("missing Jwt:Secret");
            }
        }

        [HttpGet]
        public object Generate(string payload)
        {
            if(string.IsNullOrWhiteSpace(payload))
            {
                return "Payload is null or white space";
            }

            var jObject = JObject.Parse(payload);
            var builder = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(this.secret)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddSeconds(this.expiration).ToUnixTimeSeconds());
            foreach(var item in jObject)
            {
                builder.AddClaim(item.Key, item.Value);
            }

            return builder.Build();
        }

        [HttpGet]
        public object Identity(string token)
        {
            try
            {
                var json = new JwtBuilder()
                    .WithSecret(this.secret)
                    .MustVerifySignature()
                    .Decode(token);                    
                return json;
            }
            catch (TokenExpiredException)
            {
                return "Token has expired";
            }
            catch (SignatureVerificationException)
            {
                return "Token has invalid signature";
            }
        }
    }
}