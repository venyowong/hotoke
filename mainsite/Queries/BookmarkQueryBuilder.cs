using System.Collections.Generic;

namespace Hotoke.MainSite.Queries
{
    public static class BookmarkQueryBuilder
    {
        public static object BuildKeywordQuery(string userId, string keyword)
        {
            return new
            {
                size = 5,
                query = new Dictionary<string, object>
                {
                    {"bool", new
                    {
                        must = new object[]
                        {
                            new{term = new{user_id = userId}},
                            new{query_string = new{query = keyword}}
                        }
                    }}
                }
            };
        }
    }
}