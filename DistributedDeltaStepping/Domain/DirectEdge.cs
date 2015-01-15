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
            this.U = new Vertice();
            this.V = new Vertice();
        }

        public Vertice U { get; set; }
        public Vertice V { get; set; }

        public double Cost { get; set; }

        [Serializable]
        public class Vertice
        {
            public long Id { get; set; }
            public double DistanceToRoot { get; set; }
        }
    }
}
