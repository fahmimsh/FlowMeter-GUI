using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMeterLoger
{
    class JSON_FORMAT_TCP
    {
        public Int32 devID { get; set; }
        public bool onoff { get; set; }
        public Int32 mode { get; set; }
        public Double LPM { get; set; }
        public Double setvol { get; set; }
        public Double vol { get; set; }
        public bool log { get; set; }
    }
    class JSON_FORMAT_SEND
    {
        public Int32 devID { get; set; }
        public bool onoff { get; set; }
        public Double setvol { get; set; }
    }
}