using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace elbtoes
{
    internal class S3Repository : IDisposable
    {
        readonly AmazonS3Client _client;

        public S3Repository(RegionEndpoint region)
        {
            _client = new AmazonS3Client(region);
        }

        public async Task PostFileNamesAsync(
            string bucketName,
            string prefix,
            ITargetBlock<string> target)
        {
            string token = null;
            while (true)
            {
                var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    MaxKeys = 100,
                    ContinuationToken = token,
                });
                foreach (var obj in response.S3Objects)
                {
                    await target.SendAsync(obj.Key);
                }
                if (!response.IsTruncated) break;
                token = response.NextContinuationToken;

            }
        }

        public async Task<TempFile> DownloadFileAsync(string bucketName, string key)
        {
            using (var response = await _client.GetObjectAsync(bucketName, key))
            {
                var tempFile = new TempFile();
                try
                {
                    using (var fs = tempFile.OpenWrite())
                    {
                        await response.ResponseStream.CopyToAsync(fs);
                    }
                }
                catch
                {
                    tempFile.Dispose();
                    throw;
                }
                return tempFile;
            }
        }

        public void Dispose() => _client.Dispose();
    }
}
