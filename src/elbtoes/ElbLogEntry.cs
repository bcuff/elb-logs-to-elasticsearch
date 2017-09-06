using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sprache;

namespace elbtoes
{
    public class ElbLogEntry
    {
        public static IResult<ElbLogEntry> TryParse(string line) => LogEntry.TryParse(line);

        static readonly Parser<string> NonWhitespaceToken = Parse.Token(Parse.AnyChar.Until(Parse.WhiteSpace)).Select(v => new string(v.ToArray()));

        static readonly Parser<double> DoubleToken = Parse.Token(Parse.Decimal.Select(n => double.Parse(n)));

        static readonly Parser<long> LongToken = Parse.Token(
            from sign in Parse.Optional(Parse.Char('-'))
            from number in Parse.Number
            select long.Parse((sign.IsDefined ? "-" : "") + number));

        static readonly Parser<int> StatusCode = Parse.Number.Select(i => int.Parse(i));

        static readonly Parser<int?> OptionalStatusCodeToken = Parse.Token(
            StatusCode.Select(i => (int?)i)
            .Or(
                Parse.Char('-').Then(c => Parse.Optional(StatusCode.Select(code => (int?)-code)).Select(o => o.IsDefined? o.Get() : null))
            ));

        static readonly Parser<ElbLogEntry> LogEntry =
            from timestamp in NonWhitespaceToken
            from elb_name in NonWhitespaceToken
            from client_endpoint in NonWhitespaceToken
            from backend_endpoint in NonWhitespaceToken
            from request_processing_time in DoubleToken
            from backend_processing_time in DoubleToken
            from response_processing_time in DoubleToken
            from elb_status_code in OptionalStatusCodeToken
            from backend_status_code in OptionalStatusCodeToken
            from received_bytes in LongToken
            from sent_bytes in LongToken
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
            };

        public string timestamp;
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
        // todo - request
        public string user_agent;
        public string ssl_cypher;
        public string ssl_protocol;
    }
}
