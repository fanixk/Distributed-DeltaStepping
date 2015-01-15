using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDeltaStepping.Domain
{
    [Serializable]
    public class DirectEdge
    {
        public long U { get; set; }
        public long V { get; set; }

        public double Cost { get; set; }
    }
}
