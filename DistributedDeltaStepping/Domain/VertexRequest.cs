using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDeltaStepping.Domain
{
    [Serializable]
    public class VertexRequest
    {
        public long VertexId { get; set; }
        public int RequestProcessorRank { get; set; }

        public Vertex ResponseVertex { get; set; }
    }
}
