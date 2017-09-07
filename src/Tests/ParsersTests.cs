using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using elbtoes;
using Sprache;

namespace Tests
{
    public class ParsersTests
    {
        [Theory]
        [InlineData(@"""foobar""remainder", "foobar", "remainder")]
        [InlineData(@"""foo \\ \""bar\""""remainder", @"foo \ ""bar""", "remainder")]
        public void QuotedString_should_match_expected_output(string input, string output, string remainder = "")
        {
            var result = Parsers.QuotedString.TryParse(input);
            Assert.True(result.WasSuccessful, result.ToString());
            Assert.Equal(result.Value, output);
            var r = result.Remainder;
            var remainingText = r.Source.Substring(r.Position);
            Assert.Equal(remainder, remainingText);
        }
    }
}
