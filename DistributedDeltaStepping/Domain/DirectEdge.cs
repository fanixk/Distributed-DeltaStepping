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
        public DirectEdge()
        {
            this.U = new Vertex();
            this.V = new Vertex();
        }

        public Vertex U { get; set; }
        public Vertex V { get; set; }

        public int Cost { get; set; }
    }

    [Serializable]
    public class Vertex
    {
        public long Id { get; set; }
        public int DistanceToRoot { get; set; }
    }
}
