using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Hotoke.Common;
using Hotoke.Common.Entities;
using Hotoke.MainSite.Models;
using StanSoft;

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
            catch(Exception)
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

        public static bool ContainsAny(this string str, IEnumerable<string> strs)
        {
            if(string.IsNullOrWhiteSpace(str) || strs == null || strs.Count() <= 0)
            {
                return false;
            }

            foreach(var s in strs)
            {
                if(str.Contains(s))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetMd5Hash(this string input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            using(MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}