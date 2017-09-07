using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace elbtoes
{
    public class ProgramOptions
    {
        [Option('r', "region", Default = "us-east-1", HelpText = "The S3 region that the bucket is in. us-east-1 by default")]
        public string Region { get; set; }

        [Option('b', "bucket", HelpText = "The S3 bucket name", Required = true)]
        public string BucketName { get; set; }

        [Option('p', "prefix", HelpText = "The S3 key prefix", Required = true)]
        public string Prefix { get; set; }

        [Option('d', "dest", HelpText = "The elasticsearch URL", Required = true)]
        public string Destination { get; set; }
    }
}
