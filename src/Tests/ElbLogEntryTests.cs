using System;
using System.Globalization;
using Xunit;
using elbtoes;

namespace Tests
{
    public class ElbLogEntryTests
    {
        const string httpSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.000073 0.001048 0.000057 200 200 0 29 ""GET http://www.example.com:80/ HTTP/1.1"" ""curl/7.38.0"" - -";
        const string httpsSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.000086 0.001048 0.001337 200 200 0 57 ""GET https://www.example.com:443/ HTTP/1.1"" ""curl/7.38.0"" DHE-RSA-AES128-SHA TLSv1.2";
        const string httpsCustomSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.000086 0.001048 0.001337 200 200 0 57 ""GET https://www.example.com:443/path1/path2?query1=value1&query2=value2 HTTP/1.1"" ""curl/7.38.0"" DHE-RSA-AES128-SHA TLSv1.2";
        const string tcpSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.001069 0.000028 0.000041 - - 82 305 ""- - - "" ""-"" - -";
        const string sslSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.001065 0.000015 0.000023 - - 57 502 ""- - - "" ""-"" ECDHE-ECDSA-AES128-GCM-SHA256 TLSv1.2";
        static readonly DateTime _sampleTimestamp = DateTime.Parse("2015-05-13T23:39:43.945958Z", null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        static ElbLogEntry Entry(string sample)
        {
            var result = ElbLogEntry.TryParse(sample);
            Assert.NotNull(result);
            if (!result.WasSuccessful)
            {
                var message = $"Failed at line={result.Remainder.Line} col={result.Remainder.Column}\r\n{result}";
                Assert.True(result.WasSuccessful, message);
            }
            return result.Value;
        }

        [Theory]
        [InlineData(httpSample)]
        [InlineData(httpsSample)]
        [InlineData(tcpSample)]
        [InlineData(sslSample)]
        public void timestamp_matches_samples(string sample)
        {
            Assert.Equal(DateTimeKind.Utc, Entry(sample).timestamp.Kind);
            Assert.Equal(_sampleTimestamp, Entry(sample).timestamp);
        }

        [Theory]
        [InlineData(httpSample, "my-loadbalancer")]
        [InlineData(httpsSample, "my-loadbalancer")]
        [InlineData(tcpSample, "my-loadbalancer")]
        [InlineData(sslSample, "my-loadbalancer")]
        public void elb_name_matches_samples(string sample, string elb_name)
        {
            Assert.Equal(elb_name, Entry(sample).elb_name);
        }

        [Theory]
        [InlineData(httpSample, "192.168.131.39:2817")]
        [InlineData(httpsSample, "192.168.131.39:2817")]
        [InlineData(tcpSample, "192.168.131.39:2817")]
        [InlineData(sslSample, "192.168.131.39:2817")]
        public void client_endpoint_matches_samples(string sample, string client_endpoint)
        {
            Assert.Equal(client_endpoint, Entry(sample).client_endpoint);
        }

        [Theory]
        [InlineData(httpSample, "10.0.0.1:80")]
        [InlineData(httpsSample, "10.0.0.1:80")]
        [InlineData(tcpSample, "10.0.0.1:80")]
        [InlineData(sslSample, "10.0.0.1:80")]
        public void backend_endpoint_matches_samples(string sample, string backend_endpoint)
        {
            Assert.Equal(backend_endpoint, Entry(sample).backend_endpoint);
        }

        [Theory]
        [InlineData(httpSample, 0.000073)]
        [InlineData(httpsSample, 0.000086)]
        [InlineData(tcpSample, 0.001069)]
        [InlineData(sslSample, 0.001065)]
        public void request_processing_time_matches_samples(string sample, double request_processing_time)
        {
            Assert.Equal(request_processing_time, Entry(sample).request_processing_time);
        }

        [Theory]
        [InlineData(httpSample, 0.001048)]
        [InlineData(httpsSample, 0.001048)]
        [InlineData(tcpSample, 0.000028)]
        [InlineData(sslSample, 0.000015)]
        public void backend_processing_time_matches_samples(string sample, double backend_processing_time)
        {
            Assert.Equal(backend_processing_time, Entry(sample).backend_processing_time);
        }

        [Theory]
        [InlineData(httpSample, 0.000057)]
        [InlineData(httpsSample, 0.001337)]
        [InlineData(tcpSample, 0.000041)]
        [InlineData(sslSample, 0.000023)]
        public void response_processing_time_matches_samples(string sample, double response_processing_time)
        {
            Assert.Equal(response_processing_time, Entry(sample).response_processing_time);
        }

        [Theory]
        [InlineData(httpSample, 200)]
        [InlineData(httpsSample, 200)]
        [InlineData(tcpSample, null)]
        [InlineData(sslSample, null)]
        public void elb_status_code_matches_samples(string sample, int? elb_status_code)
        {
            Assert.Equal(elb_status_code, Entry(sample).elb_status_code);
        }

        [Theory]
        [InlineData(httpSample, 200)]
        [InlineData(httpsSample, 200)]
        [InlineData(tcpSample, null)]
        [InlineData(sslSample, null)]
        public void backend_status_code_matches_samples(string sample, int? backend_status_code)
        {
            Assert.Equal(backend_status_code, Entry(sample).backend_status_code);
        }

        [Theory]
        [InlineData(httpSample, 0)]
        [InlineData(httpsSample, 0)]
        [InlineData(tcpSample, 82)]
        [InlineData(sslSample, 57)]
        public void received_bytes_matches_samples(string sample, long received_bytes)
        {
            Assert.Equal(received_bytes, Entry(sample).received_bytes);
        }

        [Theory]
        [InlineData(httpSample, 29)]
        [InlineData(httpsSample, 57)]
        [InlineData(tcpSample, 305)]
        [InlineData(sslSample, 502)]
        public void sent_bytes_matches_samples(string sample, long sent_bytes)
        {
            Assert.Equal(sent_bytes, Entry(sample).sent_bytes);
        }

        [Theory]
        [InlineData(httpSample, true)]
        [InlineData(httpsSample, true)]
        [InlineData(tcpSample, false)]
        [InlineData(sslSample, false)]
        public void request_exists_on_http_samples_and_doesnt_on_other_samples(string sample, bool exists)
        {
            Assert.Equal(exists, Entry(sample).request != null);
        }

        [Theory]
        [InlineData(httpSample, "GET")]
        [InlineData(httpsSample, "GET")]
        public void request_method_matches_on_http_samples(string sample, string method)
        {
            Assert.Equal(method, Entry(sample).request.method);
        }

        [Theory]
        [InlineData(httpSample, "http")]
        [InlineData(httpsSample, "https")]
        public void request_scheme_matches_on_http_samples(string sample, string scheme)
        {
            Assert.Equal(scheme, Entry(sample).request.scheme);
        }

        [Theory]
        [InlineData(httpSample, "www.example.com")]
        [InlineData(httpsSample, "www.example.com")]
        public void request_host_matches_on_http_samples(string sample, string host)
        {
            Assert.Equal(host, Entry(sample).request.host);
        }

        [Theory]
        [InlineData(httpSample, "80")]
        [InlineData(httpsSample, "443")]
        public void request_port_matches_on_http_samples(string sample, string port)
        {
            Assert.Equal(port, Entry(sample).request.port);
        }

        [Theory]
        [InlineData(httpSample, "HTTP/1.1")]
        [InlineData(httpsSample, "HTTP/1.1")]
        public void request_version_matches_on_http_samples(string sample, string version)
        {
            Assert.Equal(version, Entry(sample).request.version);
        }

        [Fact]
        public void request_path_is_indexed_properly()
        {
            var entry = Entry(httpsCustomSample);
            Assert.Equal(2, entry.request.path.Count);
            Assert.Equal("path1", entry.request.path["0"]);
            Assert.Equal("path2", entry.request.path["1"]);
        }

        [Fact]
        public void request_query_is_indexed_properly()
        {
            var entry = Entry(httpsCustomSample);
            Assert.Equal(2, entry.request.query.Count);
            Assert.Equal("value1", entry.request.query["query1"]);
            Assert.Equal("value2", entry.request.query["query2"]);
        }

    }
}
