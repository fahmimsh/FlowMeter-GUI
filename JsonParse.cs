using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMeterFactory
{
    class JsonParseStatusOrLog
    {
        public double ltr { get; set; }
        public double sltr { get; set; }
        public double lpm { get; set; }
        public bool onf { get; set; }
        public bool am { get; set; }
        public int tank { get; set; }
        public int src { get; set; }
    }
    class JsonParseSetOnOff
    {
        public bool onf { get; set; }
    }
    class JsonParseTankSW
    {
        public int tank { get; set; }
    }
    class JsonParseSrcSW
    {
        public int src { get; set; }
    }
    class JsonParseSltr
    {
        public double sltr { get; set; }
    }
}
