using System;
using System.Collections.Generic;
using System.Text;

namespace AzureIotCommon
{
    public class Telemetry
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public StatusType Status { get; set; }

    }
}
