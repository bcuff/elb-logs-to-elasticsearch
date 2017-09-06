using System;
using Xunit;
using elbtoes;

namespace Tests
{
    public class ElbLogEntryTests
    {
        const string httpSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.000073 0.001048 0.000057 200 200 0 29 ""GET http://www.example.com:80/ HTTP/1.1"" ""curl/7.38.0"" - -";
        const string httpsSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.000086 0.001048 0.001337 200 200 0 57 ""GET https://www.example.com:443/ HTTP/1.1"" ""curl/7.38.0"" DHE-RSA-AES128-SHA TLSv1.2";
        const string tcpSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.001069 0.000028 0.000041 - - 82 305 ""- - - "" ""-"" - -";
        const string sslSample = @"2015-05-13T23:39:43.945958Z my-loadbalancer 192.168.131.39:2817 10.0.0.1:80 0.001065 0.000015 0.000023 - - 57 502 ""- - - "" ""-"" ECDHE-ECDSA-AES128-GCM-SHA256 TLSv1.2";

        public ElbLogEntryTests()
        {
        }

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
        [InlineData(httpSample, "2015-05-13T23:39:43.945958Z")]
        [InlineData(httpsSample, "2015-05-13T23:39:43.945958Z")]
        [InlineData(tcpSample, "2015-05-13T23:39:43.945958Z")]
        [InlineData(sslSample, "2015-05-13T23:39:43.945958Z")]
        public void timestamp_matches_samples(string sample, string timestamp)
        {
            Assert.Equal(timestamp, Entry(sample).timestamp);
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

    }
}
