using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sprache;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace elbtoes
{
    public class ElbLogEntry
    {
        static readonly Encoding _encoding = new UTF8Encoding(false);
        static readonly char[] _padding = new[] { '=' };
        public static IResult<ElbLogEntry> TryParse(string line)
        {
            var result = LogEntry.TryParse(line);
            if (!result.WasSuccessful) return result;
            byte[] hash;
            using (var algo = SHA256.Create())
            {
                var bytes = _encoding.GetBytes(line);
                hash = algo.ComputeHash(bytes);
            }
            result.Value.id = Convert.ToBase64String(hash)
                .TrimEnd(_padding)
                .Replace('+', '-')
                .Replace('/', '_');
            return result;
        }

        static readonly Parser<ElbLogEntry> LogEntry =
            from timestamp in Parse.Token(Parsers.Timestamp)
            from elb_name in Parsers.NonWhitespaceToken
            from client_endpoint in Parsers.NonWhitespaceToken
            from backend_endpoint in Parsers.NonWhitespaceToken
            from request_processing_time in Parsers.DoubleToken
            from backend_processing_time in Parsers.DoubleToken
            from response_processing_time in Parsers.DoubleToken
            from elb_status_code in Parsers.OptionalStatusCodeToken
            from backend_status_code in Parsers.OptionalStatusCodeToken
            from received_bytes in Parsers.LongToken
            from sent_bytes in Parsers.LongToken
            from http in Parse.Token(Parsers.Quoted(HttpInfo.Parser))
            from user_agent in Parse.Token(Parsers.QuotedString)
            from ssl_cypher in Parsers.NonWhitespaceToken
            from ssl_protocol in Parsers.NonWhitespaceToken
            select new ElbLogEntry
            {
                timestamp = timestamp,
                elb_name = elb_name,
                client_endpoint = client_endpoint,
                backend_endpoint = backend_endpoint,
                request_processing_time = request_processing_time,
                backend_processing_time = backend_processing_time,
                response_processing_time = response_processing_time,
                elb_status_code = elb_status_code,
                backend_status_code = backend_status_code,
                received_bytes = received_bytes,
                sent_bytes = sent_bytes,
                request = http,
                user_agent = user_agent,
                ssl_cypher = ssl_cypher,
                ssl_protocol = ssl_protocol,
            };

        [JsonIgnore]
        public string id { get; private set; }
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime timestamp;
        public string elb_name;
        public string client_endpoint;
        public string backend_endpoint;
        public double request_processing_time;
        public double backend_processing_time;
        public double response_processing_time;
        public int? elb_status_code;
        public int? backend_status_code;
        public long received_bytes;
        public long sent_bytes;
        public HttpInfo request;
        public string user_agent;
        public string ssl_cypher;
        public string ssl_protocol;
    }
}
