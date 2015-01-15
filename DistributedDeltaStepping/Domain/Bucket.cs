using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDeltaStepping.Domain
{
    public class Bucket
    {
        public Bucket()
        {
            DirectEdges = new List<DirectEdge>();
        }

        public List<DirectEdge> DirectEdges
        {
            get;
            set;
        }
    }
}
