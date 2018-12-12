using System;
using System.Globalization;
using System.Linq;

namespace Hotoke.Common
{
    public static class StringUtility
    {
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
    }
}