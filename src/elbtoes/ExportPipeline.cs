using Amazon;
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
        readonly S3Repository _s3;

        public ExportPipeline(ProgramOptions options, TextWriter output, TextWriter error)
        {
            _options = options;
            _output = output;
            _error = error;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(options.Destination);
            _s3 = new S3Repository(RegionEndpoint.GetBySystemName(options.Region));
        }

        public Task RunAsync()
        {
            var concurrency = Environment.ProcessorCount;
            var batchSize = 1000;
            var download = new TransformBlock<string, TempFile>(
                key => _s3.DownloadFileAsync(_options.BucketName, key),
                new ExecutionDataflowBlockOptions { BoundedCapacity = concurrency });
            var parse = new TransformManyBlock<TempFile, ElbLogEntry>(
                (Func<TempFile, IEnumerable<ElbLogEntry>>)GetEntries,
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = concurrency, BoundedCapacity = concurrency });
            var batch = new BatchBlock<ElbLogEntry>(batchSize, new GroupingDataflowBlockOptions { BoundedCapacity = batchSize * concurrency * 2 });
            var serializer = new TransformBlock<ElbLogEntry[], HttpContent>(
                (Func<ElbLogEntry[], HttpContent>)SerializeBatch,
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = concurrency, BoundedCapacity = concurrency });
            var upload = new ActionBlock<HttpContent>(
                UploadBatch,
                new ExecutionDataflowBlockOptions { BoundedCapacity = concurrency * 2, MaxDegreeOfParallelism = concurrency });

            var linkOpts = new DataflowLinkOptions { PropagateCompletion = true };
            download.LinkTo(parse, linkOpts);
            parse.LinkTo(batch, linkOpts);
            batch.LinkTo(serializer, linkOpts);
            serializer.LinkTo(upload, linkOpts);

            _s3.PostFileNamesAsync(_options.BucketName, _options.Prefix, download)
                .ContinueWith(t =>
                {
                    var block = (IDataflowBlock)download;
                    if (t.IsFaulted)
                    {
                        block.Fault(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        block.Fault(new TaskCanceledException());
                    }
                    else
                    {
                        block.Complete();
                    }
                });
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

        private IEnumerable<ElbLogEntry> GetEntries(TempFile file)
        {
            using (file)
            using (var fs = file.OpenRead())
            using (var reader = new StreamReader(fs))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    var result = ElbLogEntry.TryParse(line);
                    if (result.WasSuccessful)
                    {
                        yield return result.Value;
                    }
                }
            }
        }

        static readonly Encoding _utf8NoBom = new UTF8Encoding(false);
        static readonly MediaTypeHeaderValue _contentType = new MediaTypeHeaderValue("application/json");

        private HttpContent SerializeBatch(ElbLogEntry[] batch)
        {
            var serializer = new JsonSerializer();
            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms, _utf8NoBom))
            {
                writer.NewLine = "\n";
                ElasticsearchUtil.WriteBulkEntries(batch, writer);
                var result = new ByteArrayContent(ms.ToArray());
                result.Headers.ContentType = _contentType;
                return result;
            }
        }

        private async Task UploadBatch(HttpContent bulkPayload)
        {
            var response = await _client.PostAsync("_bulk", bulkPayload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _error.WriteLine($"POST _bulk failed with {(int)response.StatusCode}");
                _error.WriteLine(body);
            }
        }
    }
}
