using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace elbtoes
{
    internal static class S3Util
    {
        //public static IObservable<string> GetFileNames(
        //    string bucketName,
        //    string prefix)
        //{
        //    var result = new Subject<string>();

        //    async Task GetFileNamesAsync()
        //    {
        //        using (var client = new AmazonS3Client())
        //        {
        //            string token = null;
        //            while (true)
        //            {
        //                var response = await client.ListObjectsV2Async(new ListObjectsV2Request
        //                {
        //                    BucketName = bucketName,
        //                    Prefix = prefix,
        //                    MaxKeys = 100,
        //                    ContinuationToken = token,
        //                });
        //                foreach (var obj in response.S3Objects)
        //                {
        //                    result.OnNext(obj.Key);
        //                }
        //                if (!response.IsTruncated) break;
        //                token = response.NextContinuationToken;
        //            }
        //        }
        //    }
        //}
    }
}
