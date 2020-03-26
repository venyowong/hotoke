using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Hotoke.Core.Models;
using HtmlAgilityPack;

namespace Hotoke.Core
{
    public static class Utility
    {
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

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            s = s.ToLower();
            t = t.ToLower();
            
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
 
            // Step 1
            if (n == 0)
            {
                return m;
            }
 
            if (m == 0)
            {
                return n;
            }
 
            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }
 
            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }
 
            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
 
                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static bool SimilarWith(this string str1, string str2)
        {
            var diff = LevenshteinDistance(str1, str2);
            var len = Math.Max(str1.Length, str2.Length);
            if((float)diff / (float)len <= 0.15)
            {
                return true;
            }

            return false;
        }

        public static bool Any(this string value, UnicodeCategory category)
        {
            return !string.IsNullOrWhiteSpace(value) && 
                value.Any(c => char.GetUnicodeCategory(c) == category);
        }

        public static bool HasOtherLetter(this string value) => value.Any(UnicodeCategory.OtherLetter);

        public static HtmlNodeCollection AddRange(this HtmlNodeCollection collection, HtmlNodeCollection other)
        {
            if(other == null)
            {
                return collection;
            }

            if(collection == null)
            {
                return other;
            }

            foreach(var node in other)
            {
                collection.Add(node);
            }

            return collection;
        }

        public static HtmlNodeCollection SelectAllNodes(this HtmlNode node, string selection)
        {
            if(string.IsNullOrWhiteSpace(selection))
            {
                return null;
            }

            HtmlNodeCollection result = null;
            foreach(var item in selection.Split(';'))
            {
                result = result.AddRange(node.SelectNodes(item));
            }

            return result;
        }

        public static HtmlNode SelectFirstNode(this HtmlNode node, string selection)
        {
            if(string.IsNullOrWhiteSpace(selection))
            {
                return null;
            }

            foreach(var item in selection.Split(';'))
            {
                var result = node.SelectSingleNode(item);
                if(result != null)
                {
                    return result;
                }
            }

            return null;
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