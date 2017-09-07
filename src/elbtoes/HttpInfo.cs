using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Sprache;

namespace elbtoes
{
    public class HttpInfo
    {
        static Parser<HttpInfo> Empty = Parse.Token(Parse.Char('-')).Repeat(3).Return(default(HttpInfo));

        static Dictionary<string, string> GetPath(string path)
        {
            int i = 0;
            return path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(v => i++.ToString());
        }

        static Dictionary<string, string> GetQuery(string query)
        {
            var values = HttpUtility.ParseQueryString(query);
            var q = from string key in values.Keys
                    let theValues = values.GetValues(key)
                    where theValues != null && theValues.Length > 0
                    select new KeyValuePair<string, string>(key, theValues[0]);
            return new Dictionary<string, string>(q);
        }

        public static Parser<HttpInfo> Parser = Empty.Or(
            from method in Parsers.NonWhitespaceToken
            from uri in Parse.AnyChar.Until(Parse.WhiteSpace).TryParse(u => new Uri(new string(u.ToArray())), "a valid URI")
            from version in Parsers.NonWhitespaceToken
            select new HttpInfo
            {
                method = method,
                scheme = uri.Scheme,
                host = uri.Host,
                port = uri.Port.ToString(),
                path = GetPath(uri.AbsolutePath),
                query = GetQuery(uri.Query),
                version = version,
            }
        );

        public string method { get; private set; }
        public string scheme { get; private set; }
        public string host { get; private set; }
        public string port { get; private set; }
        public Dictionary<string, string> path { get; private set; }
        public Dictionary<string, string> query { get; private set; }
        public string version { get; private set; }
    }
}
