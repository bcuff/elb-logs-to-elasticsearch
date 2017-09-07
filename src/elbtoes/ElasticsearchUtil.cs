using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace elbtoes
{
    public static class ElasticsearchUtil
    {
        static readonly JsonSerializer _serializer = new JsonSerializer();

        public static  void WriteBulkEntries(ElbLogEntry[] batch, TextWriter writer)
        {
            foreach (var entry in batch)
            {
                WriteBulkEntry(entry, writer);
            }
        }

        public static void WriteBulkEntry(ElbLogEntry entry, TextWriter writer)
        {
            // todo - use time based index
            var header = new { index = new { _index = "elb-logs", _type = "elb-log-entry", _id = Guid.NewGuid() } };
            _serializer.Serialize(writer, header);
            writer.WriteLine();
            _serializer.Serialize(writer, entry);
            writer.WriteLine();
        }
    }
}
