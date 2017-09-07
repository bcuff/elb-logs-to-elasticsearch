using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sprache;

namespace elbtoes
{
    public static class Parsers
    {
        public static Parser<T> DashOrToken<T>(Parser<T> innerParser) => Parse.Token(
                Parse.Char('-').Return(default(T)).Or(innerParser));
        public static readonly Parser<DateTime> Timestamp = Parse.AnyChar.Except(Parse.WhiteSpace).AtLeastOnce()
            .TryParse(c => DateTime.Parse(new string(c.ToArray()), null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), "a valid timestamp");
        public static readonly Parser<string> NonWhitespaceToken = Parse.Token(Parse.AnyChar.Except(Parse.WhiteSpace).AtLeastOnce()).Select(v => new string(v.ToArray()));

        public static readonly Parser<double> DoubleToken = Parse.Token(Parse.Decimal.Select(n => double.Parse(n)));

        public static readonly Parser<long> LongToken = Parse.Token(
            from sign in Parse.Optional(Parse.Char('-'))
            from number in Parse.Number
            select long.Parse((sign.IsDefined ? "-" : "") + number));

        public static readonly Parser<int> StatusCode = Parse.Number.Select(i => int.Parse(i));

        public static readonly Parser<int?> OptionalStatusCodeToken = Parse.Token(
            StatusCode.Select(i => (int?)i)
            .Or(
                Parse.Char('-').Then(c => Parse.Optional(StatusCode.Select(code => (int?)-code)).Select(o => o.IsDefined ? o.Get() : null))
            ));

        static readonly Parser<char> Quote = Parse.Char('"');
        static readonly Parser<char> Escape = Parse.Char('\\');

        public static readonly Parser<string> QuotedString =
            from open in Quote
            from content in Parse.Or(
                Escape.Then(_ => Escape.Or(Quote)),
                Parse.AnyChar.Except(Quote)
            ).Many()
            from close in Quote
            select new string(content.ToArray());

        public static Parser<TOut> TryParse<TIn, TOut>(this Parser<TIn> parser, Func<TIn, TOut> parse, string description)
        {
            var expectations = new[] { description };
            return input =>
            {
                var r = parser(input);
                if (!r.WasSuccessful) return Result.Failure<TOut>(r.Remainder, r.Message, r.Expectations);
                TOut result;
                try
                {
                    result = parse(r.Value);
                }
                catch (Exception e)
                {
                    return Result.Failure<TOut>(input, e.Message, expectations);
                }
                return Result.Success(result, r.Remainder);
            };
        }

        public static Parser<T> Quoted<T>(Parser<T> parser) => input =>
        {
            var result = QuotedString(input);
            if (!result.WasSuccessful) return Result.Failure<T>(result.Remainder, result.Message, result.Expectations);
            var r = parser.TryParse(result.Value);
            if (r.WasSuccessful)
            {
                return Result.Success(r.Value, result.Remainder);
            }
            return Result.Failure<T>(result.Remainder, r.Message, r.Expectations);
        };
    }
}
