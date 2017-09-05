using System;
using System.Collections.Generic;
using System.Text;

namespace elbtoes
{
    internal class ElbLogEntry
    {
        public string timestamp;
        public string elb_name;
        public string client_endpoint;
        public string backend_endpoint;
        public double request_processing_time;
        public double backend_processing_time;
        public int elb_status_code;
        public int backend_status_code;
        public long received_bytes;
        public long sent_bytes;
        // todo - request
        public string user_agent;
        public string ssl_cypher;
        public string ssl_protocol;
    }
}
