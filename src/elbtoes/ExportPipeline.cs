using Amazon;
using Elasticsearch.Net.Aws;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;

namespace elbtoes
{
    class ExportPipeline
    {
        readonly ProgramOptions _options;
        readonly TextWriter _output;
        readonly TextWriter _error;
        readonly HttpClient _client;
        readonly string _bulkUrl;
        readonly bool _signES;
        readonly S3Repository _s3;

        public ExportPipeline(ProgramOptions options, TextWriter output, TextWriter error)
        {
            _options = options;
            _output = output;
            _error = error;
            _client = new HttpClient();
            var url = new Uri(new Uri(options.Destination), "_bulk");
            _bulkUrl = url.ToString();
            _signES = url.Host.EndsWith("es.amazonaws.com");
            _s3 = new S3Repository(RegionEndpoint.GetBySystemName(options.Region));
        }

        public Task RunAsync()
        {
            var concurrency = Environment.ProcessorCount;
            var batchSize = 1000;
            var download = new TransformBlock<string, TempFile>(
                key => _s3.DownloadFileAsync(_options.BucketName, key),
                new ExecutionDataflowBlockOptions { BoundedCapacity = concurrency });
            var batch = new BatchBlock<ElbLogEntry>(batchSize, new GroupingDataflowBlockOptions { BoundedCapacity = batchSize * concurrency * 2 });
            var fileReader = new ActionBlock<TempFile>(
                file => PostEntriesAsync(file, batch),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = concurrency, BoundedCapacity = concurrency });
            var serializer = new TransformBlock<ElbLogEntry[], HttpRequestMessage>(
                (Func<ElbLogEntry[], HttpRequestMessage>)SerializeBatch,
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = concurrency, BoundedCapacity = concurrency });
            var upload = new ActionBlock<HttpRequestMessage>(
                UploadBatch,
                new ExecutionDataflowBlockOptions { BoundedCapacity = concurrency * 2, MaxDegreeOfParallelism = concurrency });

            var linkOpts = new DataflowLinkOptions { PropagateCompletion = true };
            download.LinkTo(fileReader, linkOpts);
            fileReader.PropagateCompletion(batch);
            batch.LinkTo(serializer, linkOpts);
            serializer.LinkTo(upload, linkOpts);

            _s3.PostFileNamesAsync(_options.BucketName, _options.Prefix, download)
                .PropagateCompletion(download);
            return upload.Completion;
        }

        private async Task<TempFile> DownloadAsync(string key)
        {
            _output.WriteLine($"Downloading {key}...");
            var watch = Stopwatch.StartNew();
            var result = await _s3.DownloadFileAsync(_options.BucketName, key);
            watch.Stop();
            _output.WriteLine($"Download of {key} completed. Elapsed={watch.Elapsed}");
            return result;
        }

        private async Task PostEntriesAsync(TempFile file, ITargetBlock<ElbLogEntry> target)
        {
            using (file)
            using (var fs = file.OpenRead())
            using (var reader = new StreamReader(fs, Encoding.UTF8, true, 128 << 10))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    var result = ElbLogEntry.TryParse(line);
                    if (result.WasSuccessful)
                    {
                        await target.SendAsync(result.Value);
                    }
                }
            }
        }

        static readonly Encoding _utf8NoBom = new UTF8Encoding(false);
        static readonly MediaTypeHeaderValue _contentType = new MediaTypeHeaderValue("application/json");

        private HttpRequestMessage SerializeBatch(ElbLogEntry[] batch)
        {
            var serializer = new JsonSerializer();
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, _utf8NoBom))
            {
                writer.NewLine = "\n";
                ElasticsearchUtil.WriteBulkEntries(batch, writer);
                var bytes = ms.ToArray();
                var request = new HttpRequestMessage(HttpMethod.Post, _bulkUrl);
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = _contentType;
                CredentialChainProvider.Default.Sign(request, bytes);
                return request;
            }
        }

        private async Task UploadBatch(HttpRequestMessage bulkRequest)
        {
            var response = await _client.SendAsync(bulkRequest);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _error.WriteLine($"POST _bulk failed with {(int)response.StatusCode}");
                _error.WriteLine(body);
            }
        }
    }
}
